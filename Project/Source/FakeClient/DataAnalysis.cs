// DataAnalysis.cs
// Created by xiaojl Aug/25/2022
// 数据分析组件

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class DataAnalysis : Component
{
    public override void Init(Core core)
    {
        base.Init(core);

        _headers = new string[] {
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
            "SendFrameSyncRsp",
            "BroadcastFrameSync",
            "StopFrameSyncReq",
            "StopFrameSyncRsp",
            "LeaveRoomReq",
            "LeaveRoomRsp",
        };
    }

    public void Export()
    {
        _sb.Clear();
        for (int i = 0; i < _headers.Length; i++)
        {
            var key = _headers[i];
            _sb.Append(key);
            _sb.Append(',');
        }
        _sb.AppendLine();

        var game = (Game)_core;
        var clients = game.GetAllClients();
        foreach (var client in clients)
        {
            Dictionary<string, int> data;
            lock(client)
            {
                data = client.Export();
            }

            for (int i = 0; i < _headers.Length; i++)
            {
                var key = _headers[i];
                int val = -1;
                if (data.ContainsKey(key))
                    val = data[key];

                _sb.Append(val);
                _sb.Append(',');
            }
            _sb.AppendLine();
        }

        File.WriteAllText(_path, _sb.ToString());
    }

    private string[] _headers;
    private StringBuilder _sb = new StringBuilder();
    private string _path = Path.Combine(Environment.CurrentDirectory, "data.csv");
}