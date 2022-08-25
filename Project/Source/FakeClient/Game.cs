using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using BudEngine.NetEngine.src.Util;

public class Game : Core
{
    public Game()
    {
        Add("ConsoleInput", new ConsoleInput());
        Add("DataAnalysis", new DataAnalysis());
    }

    public override void Initialize()
    {
        base.Initialize();

        Pb.Init();
        Http.Init();

        var globalJson = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Configs/global.json"));
        var globalConfig = JsonConvert.DeserializeObject<GlobalInfo>(globalJson);
        var userConfig = CsvReader.ReadCsv<UserInfo>("users");

        var httpUrl = globalConfig.httpUrl;
        var mapInfo = globalConfig.mapInfo;
        var userInfos = userConfig.Values.ToArray();

        for (int i = 0; i < userInfos.Length; i++)
        {
            var userInfo = userInfos[i];

            var client = new FakeClient();
            _clients[userInfo.uid] = client;

            client.Init(httpUrl, mapInfo, userInfo);
        }
    }

    public override void RunOneFrame(int elapsedTime)
    {
        base.RunOneFrame(elapsedTime);
    }

    public void Start()
    {
        FrameDriver.Instance.RunOneFrame += RunOneFrame;

        Get<ConsoleInput>().Start();

        var clients = GetAllClients();
        foreach (FakeClient client in clients)
        {
            _workingThread.Dispatch(() => {
                client.Go();
            });
        }

        FrameDriver.Instance.Start();
    }

    public FakeClient[] GetAllClients()
    {
        return _clients.Values.ToArray();
    }

    public string LetUserLeaveRoom(string userId)
    {
        FakeClient client;
        if (_clients.TryGetValue(userId, out client))
        {
            client.Quit();
            return string.Format("Waiting for user leave room: {0}.", userId);
        }

        return string.Format("User leave room failed. {0} does not exist.", userId);
    }

    public string LetAllUserLeaveRoom()
    {
        foreach (var kvp in _clients)
        {
            var client = kvp.Value;
            client.Quit();
        }

        return string.Format("Waiting for all user leave room.");
    }

    private QueuedThread _workingThread = new QueuedThread(24);
    private Dictionary<string, FakeClient> _clients = new Dictionary<string, FakeClient>();
}
