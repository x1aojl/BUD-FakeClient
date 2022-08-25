using System;
using System.Collections.Generic;
using BudEngine.NetEngine;
using BudEngine.NetEngine.src.Util;
using Newtonsoft.Json;

public enum RecChatType
{
    GetItems = 5,
}

public partial class FakeClient
{
    public void Init(string httpUrl, MapInfo mapInfo, UserInfo userInfo)
    {
        FrameDriver.Instance.RunOneFrame += RunOneFrame;

        this._httpUrl = httpUrl;
        this._mapInfo = mapInfo;
        this._userInfo = userInfo;
    }

    public void Go()
    {
        if (_tryTime == 0)
            _startTime = DateTime.Now;

        if (++_tryTime > MAX_TRY_TIME)
            return;

        GetGameSession();
    }

    public void Quit()
    {
        StopFrameSync();
        LeaveRoom();
    }

    #region 进房流程
    private void GetGameSession()
    {
        var url = string.Format("{0}/{1}", _httpUrl, "engine-match/getGameSession");
        var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_mapInfo));
        var headers = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_userInfo));

        EventSystem.Instance.Send(EventID.GET_GAME_SESSION_REQ, _userInfo.uid);
        Http.MakeHttpRequest(url, parameters, headers, (content) => {
            EventSystem.Instance.Send(EventID.GET_GAME_SESSION_RSP, _userInfo.uid);
            HandleGetGameSession(JsonConvert.DeserializeObject<Dictionary<string, object>>(content));
        }, (content) => {
            Go();
        });
    }

    private void HandleGetGameSession(Dictionary<string, object> sessions)
    {
        _sessions = sessions;

        _socket1 = new Socket(_ip, _port);
        _socket2 = new Socket(_ip, _port + 2000);

        _socket1.onConnect = HandleRoomConnect;
        _socket2.onConnect = HandleFrameConnect;

        _socket1.onResponse = HandleRoomResponse;
        _socket2.onResponse = HandleFrameResponse;
        _socket1.onBroadcast = HandleRoomBroadcast;
        _socket2.onBroadcast = HandleFrameBroadcast;

        _socket1.Connect();
    }

    private void EnterRoom()
    {
        var playerInfo = new PlayerInfo();
        playerInfo.Id = _userInfo.uid;
        playerInfo.Name = "";
        playerInfo.CustomProfile = "";
        playerInfo.CustomPlayerStatus = 1;
        playerInfo.ImageChosenDataJson = "";
        playerInfo.Lang = "";
        playerInfo.Locale = "US";

        var req = new EnterRoomReq();
        req.RoomName = "enter_room";
        req.RoomType = _mapInfo.roomType;
        req.MaxPlayers = (ulong)_mapInfo.maxPlayerCount;
        req.IsPrivate = _mapInfo.isPrivate == 1;
        req.CustomProperties = "";
        req.PlayerInfo = playerInfo;
        req.SessionId = _sessionId;
        req.RoomId = _roomId;
        req.GameData = "{\"IsPvp\":\"0\",\"ReadyTime\":9,\"GameDuration\":0,\"WinType\":0,\"OpenBlood\":false,\"HasLeaderBoard\":false,\"InitBlood\":100.0,\"IsOpenBaggage\":false,\"Team\":\"\",\"damageType\":[0],\"Seq\":\"\"}";

        if (_socket1 != null)
        {
            _socket1.SendRequest(ClientSendServerReqCmd.ECmdEnterRoomReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.ENTER_ROOM_REQ, _userInfo.uid);
                else
                    Go();
            });
        }
    }

    private void HandleEnterRoom()
    {
        if (_socket2 != null)
            _socket2.Connect();

        GetItems();
    }

    private void GetItems()
    {
        var getItemsReq = new Dictionary<string, object>();
        getItemsReq.Add("mapId", _mapInfo.mapId);

        var roomChatData = new Dictionary<string, object>();
        roomChatData.Add("msgType", (int)RecChatType.GetItems);
        roomChatData.Add("data", JsonConvert.SerializeObject(getItemsReq));
        roomChatData.Add("requestSeq", "");

        List<string> recvPlayerList = new List<string>();
        recvPlayerList.Add(_userInfo.uid);

        var req = new SendToClientReq();
        req.PlayerId = _userInfo.uid;
        req.RoomId = _roomId;
        req.Msg = JsonConvert.SerializeObject(roomChatData);
        req.RecvPlayerList.AddRange(recvPlayerList);

        if (_socket1 != null)
        {
            _socket1.SendRequest(ClientSendServerReqCmd.ECmdRoomChatReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.GET_ITEMS_REQ, _userInfo.uid);
                else
                    Go();
            });
        }
    }

    private void HandleGetItems(string data)
    {
        //ConsoleLogger.Info(string.Format("Get items success. data = {0}", data));
    }

    private void StartFrameSync()
    {
        var req = new StartFrameSyncReq();
        req.RoomId = _roomId;
        req.PlayerId = _userInfo.uid;

        if (_socket2 != null)
        {
            _socket2.SendRequest(ClientSendServerReqCmd.ECmdStartFrameSyncReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.START_FRAME_SYNC_REQ, _userInfo.uid);
                else
                    Go();
            });
        }
    }

    private void HandleStartFrameSync()
    {
        _isCanSendFrame = true;
    }

    private void StopFrameSync()
    {
        _isCanSendFrame = false;

        var req = new StopFrameSyncReq();
        req.RoomId = _roomId;
        req.PlayerId = _userInfo.uid;

        if (_socket2 != null)
        {
            _socket2.SendRequest(ClientSendServerReqCmd.ECmdStopFrameSyncReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.STOP_FRAME_SYNC_REQ, _userInfo.uid);
            });
        }
    }

    private void HandleStopFrameSync()
    {
        ConsoleLogger.Info(string.Format("User({0}) stop frame sync success.", _userInfo.uid));
    }

    private void SendFrame()
    {
        var req = new SendFrameReq();
        req.RoomId = _roomId;
        req.Item = GetFrameItem();

        if (_socket2 != null)
        {
            _socket2.SendRequest(ClientSendServerReqCmd.ECmdRelaySendFrameReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.SEND_FRAME_SYNC_REQ, _userInfo.uid);
                else
                    Go();
            });
        }
    }

    private void HandleBroadcastFrameSync()
    {
        //ConsoleLogger.Info(string.Format("User({0}) receive frame data.", _userInfo.uid));
    }

    private void LeaveRoom()
    {
        var req = new LeaveRoomReq();
        req.PlayerId = _userInfo.uid;
        req.RoomId = _roomId;

        if (_socket1 != null)
        {
            _socket1.SendRequest(ClientSendServerReqCmd.ECmdQuitRoomReq, _userInfo.uid, req, (ok) => {
                if (ok)
                    EventSystem.Instance.Send(EventID.LEAVE_ROOM_REQ, _userInfo.uid);
            });
        }
    }

    private void HandleLeaveRoom()
    {
        ConsoleLogger.Info(string.Format("User({0}) has left the room", _userInfo.uid));
    }

    private void RunOneFrame(int deltaTime)
    {
        _lastFrameTime += deltaTime * 1000;

        if (_isCanSendFrame && _lastFrameTime >= FRAME_STEP)
        {
            SendFrame();
            _lastFrameTime %= FRAME_STEP;
        }
    }

    private FrameItem GetFrameItem()
    {
        string data =
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{0 * 10000f:0}|" +
            $"{false}|" +
            $"{false}|" +
            $"{_mapInfo.mapId}|" +
            $"{false}|" +
            $"{false}|" +
            $"{false}|" +
            $"{false}|" +
            $"{1}|" +
            $"{1}";

        var startTime = new DateTime(1970, 1, 1);
        var currentTime = DateTime.Now.ToUniversalTime();
        var timeSpan = currentTime - startTime;
        var timestamp = Convert.ToUInt64(Convert.ToInt64(timeSpan.TotalMilliseconds + 0.5));

        var item = new FrameItem();
        item.PlayerId = _userInfo.uid;
        item.Data = data;
        item.Timestamp = timestamp;

        return item;
    }
    #endregion

    #region 服务器消息处理
    private void HandleRoomConnect()
    {
        EnterRoom();
    }

    private void HandleFrameConnect()
    {
        StartFrameSync();
    }

    private void HandleRoomResponse(ClientSendServerReqCmd cmd, DecodeRspResult result)
    {
        switch (cmd)
        {
            case ClientSendServerReqCmd.ECmdEnterRoomReq:
                EventSystem.Instance.Send(EventID.ENTER_ROOM_RSP, _userInfo.uid);
                HandleEnterRoom();
                break;
            case ClientSendServerReqCmd.ECmdRoomChatReq:
                EventSystem.Instance.Send(EventID.GET_ITEMS_RSP, _userInfo.uid);
                EventSystem.Instance.Send(EventID.ENTER_ROOM_COST_TIME, _userInfo.uid);
                HandleGetItems(result.Body.ToString());
                break;
            case ClientSendServerReqCmd.ECmdQuitRoomReq:
                EventSystem.Instance.Send(EventID.LEAVE_ROOM_RSP, _userInfo.uid);
                HandleLeaveRoom();
                break;
        }
    }

    private void HandleFrameResponse(ClientSendServerReqCmd cmd, DecodeRspResult result)
    {
        switch (cmd)
        {
            case ClientSendServerReqCmd.ECmdStartFrameSyncReq:
                EventSystem.Instance.Send(EventID.START_FRAME_SYNC_RSP, _userInfo.uid);
                HandleStartFrameSync();
                break;
            case ClientSendServerReqCmd.ECmdStopFrameSyncReq:
                EventSystem.Instance.Send(EventID.STOP_FRAME_SYNC_RSP, _userInfo.uid);
                HandleStopFrameSync();
                break;
        }
    }

    private void HandleRoomBroadcast(DecodeBstResult result)
    {
        var type = result.BstPacket.Type;
        switch (type)
        {
            case ServerSendClientBstType.EPushTypeNetworkState:
                //ConsoleLogger.Warn(string.Format("Receive network broadcast. response = {0}", result.Body));
                break;
            case ServerSendClientBstType.EPushTypeStartGame:
                //ConsoleLogger.Warn(string.Format("Receive start game broadcast. response = {0}", result.Body));
                break;
            case ServerSendClientBstType.EPushTypeJoinRoom:
                //ConsoleLogger.Warn(string.Format("Receive join room broadcast. response = {0}", result.Body));
                break;
            case ServerSendClientBstType.EPushTypeLeaveRoom:
                //ConsoleLogger.Warn(string.Format("Receive leave room  broadcast. response = {0}", result.Body));
                break;
        }
    }

    private void HandleFrameBroadcast(DecodeBstResult result)
    {
        var type = result.BstPacket.Type;
        switch (type)
        {
            case ServerSendClientBstType.EPushTypeRelay:
                EventSystem.Instance.Send(EventID.BROADCAST_FRAME_SYNC, _userInfo.uid);
                HandleBroadcastFrameSync();
                break;
        }
    }
    #endregion

    private string _httpUrl;
    private MapInfo _mapInfo;
    private UserInfo _userInfo;

    private Socket _socket1;
    private Socket _socket2;

    private DateTime _startTime;

    private bool _isCanSendFrame = false;
    private int _lastFrameTime;
    private const int FRAME_STEP = 60;

    private int _tryTime = 0;
    private const int MAX_TRY_TIME = 3;

    private Dictionary<string, object> _sessions = new Dictionary<string, object>();

    private string _ip { get { return _sessions != null && _sessions.ContainsKey("ipAddress") ? _sessions["ipAddress"].ToString() : ""; } }
    private int _port { get { return _sessions != null && _sessions.ContainsKey("port") ? int.Parse(_sessions["port"].ToString()) : 0; } }
    private string _roomId { get { return _sessions != null && _sessions.ContainsKey("roomId") ? _sessions["roomId"].ToString() : ""; } }
    private string _sessionId { get { return _sessions != null && _sessions.ContainsKey("sessionId") ? _sessions["sessionId"].ToString() : ""; } }
}