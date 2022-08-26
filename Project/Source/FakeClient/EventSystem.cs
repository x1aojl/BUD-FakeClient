// EventSystem.cs
// Created by xiaojl Aug/25/2022
// 事件系统

using System.Collections.Generic;

public class EventID
{
    public const string GET_GAME_SESSION_REQ = "GetGameSessionReq";
    public const string GET_GAME_SESSION_RSP = "GetGameSessionRsp";
    public const string ENTER_ROOM_REQ       = "EnterRoomReq";
    public const string ENTER_ROOM_RSP       = "EnterRoomRsp";
    public const string START_FRAME_SYNC_REQ = "StartFrameSyncReq";
    public const string START_FRAME_SYNC_RSP = "StartFrameSyncRsp";
    public const string GET_ITEMS_REQ        = "GetItemsReq";
    public const string GET_ITEMS_RSP        = "GetItemsRsp";
    public const string ENTER_ROOM_COST_TIME = "EnterRoomCostTime";
    public const string SEND_FRAME_SYNC_REQ  = "SendFrameSyncReq";
    public const string SEND_FARME_SYNC_RSP  = "SendFrameSyncRsp";
    public const string BROADCAST_FRAME_SYNC = "BroadcastFrameSync";
    public const string STOP_FRAME_SYNC_REQ  = "StopFrameSyncReq";
    public const string STOP_FRAME_SYNC_RSP  = "StopFrameSyncRsp";
    public const string LEAVE_ROOM_REQ       = "LeaveRoomReq";
    public const string LEAVE_ROOM_RSP       = "LeaveRoomRsp";
}

public class EventSystem
{
    public static EventSystem Instance = new EventSystem();

    public delegate void EventAction(params object[] args);

    public void Subscribe(string eventId, EventAction callback)
    {
        List<EventAction> lst;
        if (! _events.TryGetValue(eventId, out lst))
        {
            lst = new List<EventAction>();
            _events[eventId] = lst;
        }

        lst.Add(callback);
    }

    public void Unsubscribe(string eventId, EventAction callback)
    {
        List<EventAction> lst;
        if (_events.TryGetValue(eventId, out lst))
            lst.Remove(callback);
    }

    public void Send(string eventId, params object[] args)
    {
        List<EventAction> lst;
        if (_events.TryGetValue(eventId, out lst))
        {
            for (int i = 0; i < lst.Count; i++)
                lst[i]?.Invoke(args);
        }
    }

    private Dictionary<string, List<EventAction>> _events = new Dictionary<string, List<EventAction>>();
}