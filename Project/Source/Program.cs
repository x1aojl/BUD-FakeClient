using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using BudEngine.NetEngine.src.Util;

public class Program
{
    private static QueuedThread workerThread;
    private static List<FakeClient> _clients = new List<FakeClient>();
    private static string _analysisExportPath = @"C:\Users\Administrator\Desktop\data.csv";
    private static StringBuilder _analysisSb = new StringBuilder();
    private static string[] _analysisHeaders = new string[]{
        "GetGameSessionReq",
        "GetGameSessionRsp",
        "EnterRoomReq",
        "EnterRoomRsp",
        "StartFrameSyncReq",
        "StartFrameSyncRsp",
        "GetItemsReq",
        "GetItemsRsp",
        "EnterRoomCostTime",
        "SendFrameSyncReq",
        "BroadcastFrameSync",
    };

    private static void Main(string[] args)
    {
        Pb.Init();
        Http.Init();

        workerThread = new QueuedThread(32);

        var globalJson = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Configs/global.json"));
        var globalConfig = JsonConvert.DeserializeObject<GlobalInfo>(globalJson);
        var userConfig = CsvReader.ReadCsv<UserInfo>("users");

        var httpUrl = globalConfig.httpUrl;
        var mapInfo = globalConfig.mapInfo;
        var userInfos = userConfig.Values.ToList();

        for (int i = 0; i < userInfos.Count; i++)
        {
            var client = new FakeClient();
            client.Init(httpUrl, mapInfo, userInfos[i]);
            _clients.Add(client);
        }

        foreach (FakeClient client in _clients)
        {
            workerThread.Dispatch(() => {
                client.Go();
            });
        }

        FrameDriver.Instance.Start();
    }

    private void Export()
    {
        _analysisSb.Clear();
        for (int i = 0; i < _analysisHeaders.Length; i++)
        {
            var key = _analysisHeaders[i];
            _analysisSb.Append(key);
            _analysisSb.Append(',');
        }
        _analysisSb.AppendLine();

        foreach (var client in _clients)
        {
            var data = client.Export();

            for (int i = 0; i < _analysisHeaders.Length; i++)
            {
                var key = _analysisHeaders[i];
                int val = 0;
                if (data.ContainsKey(key))
                    val = data[key];

                _analysisSb.Append(val);
                _analysisSb.Append(',');
            }
            _analysisSb.AppendLine();
        }

        File.WriteAllText(_analysisExportPath, _analysisSb.ToString());
    }
}