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
            EventID.GET_GAME_SESSION_REQ, EventID.GET_GAME_SESSION_RSP,
            EventID.ENTER_ROOM_REQ, EventID.ENTER_ROOM_RSP,
            EventID.START_FRAME_SYNC_REQ, EventID.START_FRAME_SYNC_RSP,
            EventID.GET_ITEMS_REQ, EventID.GET_ITEMS_RSP,
            EventID.ENTER_ROOM_COST_TIME,
            EventID.SEND_FRAME_SYNC_REQ, EventID.SEND_FARME_SYNC_RSP,
            EventID.BROADCAST_FRAME_SYNC,
            EventID.STOP_FRAME_SYNC_REQ, EventID.STOP_FRAME_SYNC_RSP,
            EventID.LEAVE_ROOM_REQ, EventID.LEAVE_ROOM_RSP,
        };

        EventSystem.Instance.Subscribe(EventID.GET_GAME_SESSION_REQ, (userId) => { Record(userId, EventID.GET_GAME_SESSION_REQ); });
        EventSystem.Instance.Subscribe(EventID.GET_GAME_SESSION_RSP, (userId) => { Record(userId, EventID.GET_GAME_SESSION_RSP); });
        EventSystem.Instance.Subscribe(EventID.ENTER_ROOM_REQ,       (userId) => { Record(userId, EventID.ENTER_ROOM_REQ); });
        EventSystem.Instance.Subscribe(EventID.ENTER_ROOM_RSP,       (userId) => { Record(userId, EventID.ENTER_ROOM_RSP); });
        EventSystem.Instance.Subscribe(EventID.START_FRAME_SYNC_REQ, (userId) => { Record(userId, EventID.START_FRAME_SYNC_REQ); });
        EventSystem.Instance.Subscribe(EventID.START_FRAME_SYNC_RSP, (userId) => { Record(userId, EventID.START_FRAME_SYNC_RSP); });
        EventSystem.Instance.Subscribe(EventID.GET_ITEMS_REQ,        (userId) => { Record(userId, EventID.GET_ITEMS_REQ); });
        EventSystem.Instance.Subscribe(EventID.GET_ITEMS_RSP,        (userId) => { Record(userId, EventID.GET_ITEMS_RSP); });
        EventSystem.Instance.Subscribe(EventID.ENTER_ROOM_COST_TIME, (userId) => { Record(userId, EventID.ENTER_ROOM_COST_TIME); });
        EventSystem.Instance.Subscribe(EventID.SEND_FRAME_SYNC_REQ,  (userId) => { Record(userId, EventID.SEND_FRAME_SYNC_REQ); });
        EventSystem.Instance.Subscribe(EventID.SEND_FARME_SYNC_RSP,  (userId) => { Record(userId, EventID.SEND_FARME_SYNC_RSP); });
        EventSystem.Instance.Subscribe(EventID.BROADCAST_FRAME_SYNC, (userId) => { Record(userId, EventID.BROADCAST_FRAME_SYNC); });
        EventSystem.Instance.Subscribe(EventID.STOP_FRAME_SYNC_REQ,  (userId) => { Record(userId, EventID.STOP_FRAME_SYNC_REQ); });
        EventSystem.Instance.Subscribe(EventID.STOP_FRAME_SYNC_RSP,  (userId) => { Record(userId, EventID.STOP_FRAME_SYNC_RSP); });
        EventSystem.Instance.Subscribe(EventID.LEAVE_ROOM_REQ,       (userId) => { Record(userId, EventID.LEAVE_ROOM_REQ); });
        EventSystem.Instance.Subscribe(EventID.LEAVE_ROOM_RSP,       (userId) => { Record(userId, EventID.LEAVE_ROOM_RSP); });
    }

    public void Export()
    {
        _sb.Clear();

        // 写入首行
        _sb.Append("UserId");
        for (int i = 0; i < _headers.Length; i++)
        {
            var key = _headers[i];
            _sb.Append(key);
            _sb.Append(',');
        }
        _sb.AppendLine();

        foreach (var kvp in _datas)
        {
            var userId = kvp.Key;
            var data = kvp.Value;

            _sb.Append(userId);
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

    private void Record(string userId, string eventId)
    {
        lock(_datas)
        {
            Dictionary<string, int> dic;
            if (!_datas.TryGetValue(userId, out dic))
            {
                dic = new Dictionary<string, int>();
                _datas[userId] = dic;
            }

            int val = 0;
            if (!dic.TryGetValue(eventId, out val))
                dic[eventId] = 0;

            dic[eventId] += 1;
        }
    }

    private string[] _headers;
    private StringBuilder _sb = new StringBuilder();
    private string _path = Path.Combine(Environment.CurrentDirectory, "data.csv");
    private Dictionary<string, Dictionary<string, int>> _datas = new Dictionary<string, Dictionary<string, int>>();
}