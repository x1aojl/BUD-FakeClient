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

    private Dictionary<string, object> _sessionDic = new Dictionary<string, object>();
    private Dictionary<string, int> _analysisDic = new Dictionary<string, int>();

    public void Init(string httpUrl, MapInfo mapInfo, UserInfo userInfo)
    {
        FrameDriver.Instance.RunOneFrame += RunOneFrame;

        this._httpUrl = httpUrl;
        this._mapInfo = mapInfo;
        this._userInfo = userInfo;
    }

    #region 进房流程
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

    private void GetGameSession()
    {
        Record("GetGameSessionReq");

        var url = string.Format("{0}/{1}", _httpUrl, "engine-match/getGameSession");
        var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_mapInfo));
        var headers = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_userInfo));

        Http.MakeHttpRequest(url, parameters, headers, (content) => {
            HandleGetGameSession(JsonConvert.DeserializeObject<Dictionary<string, object>>(content));
        }, (content) => {
            Go();
        });
    }

    private void HandleGetGameSession(Dictionary<string, object> data)
    {
        Record("GetGameSessionRsp");

        _sessionDic = data;

        var ip = data["ipAddress"].ToString();
        var port = int.Parse(data["port"].ToString());

        _socket1 = new Socket(ip, port);
        _socket2 = new Socket(ip, port + 2000);

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
        req.SessionId = _sessionDic["sessionId"].ToString();
        req.RoomId = _sessionDic["roomId"].ToString();
        req.GameData = "{\"IsPvp\":\"0\",\"ReadyTime\":9,\"GameDuration\":0,\"WinType\":0,\"OpenBlood\":false,\"HasLeaderBoard\":false,\"InitBlood\":100.0,\"IsOpenBaggage\":false,\"Team\":\"\",\"damageType\":[0],\"Seq\":\"\"}";

        _socket1.SendRequest(ClientSendServerReqCmd.ECmdEnterRoomReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("EnterRoomReq");
            else
                Go();
        });
    }

    private void HandleEnterRoom()
    {
        Record("EnterRoomRsp");

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
        req.RoomId = _sessionDic["roomId"].ToString();
        req.Msg = JsonConvert.SerializeObject(roomChatData);
        req.RecvPlayerList.AddRange(recvPlayerList);

        _socket1.SendRequest(ClientSendServerReqCmd.ECmdRoomChatReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("GetItemsReq");
            else
                Go();
        });
    }

    private void HandleGetItems()
    {
        Record("GetItemsRsp");
        Record("EnterRoomCostTime", (int)DateTime.Now.Subtract(_startTime).TotalSeconds);
    }

    private void StartFrameSync()
    {
        var req = new StartFrameSyncReq();
        req.RoomId = _sessionDic["roomId"].ToString();
        req.PlayerId = _userInfo.uid;

        _socket2.SendRequest(ClientSendServerReqCmd.ECmdStartFrameSyncReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("StartFrameSyncReq");
            else
                Go();
        });
    }

    private void HandleStartFrameSync()
    {
        _isCanSendFrame = true;

        Record("StartFrameSyncRsp");
    }

    private void StopFrameSync()
    {
        _isCanSendFrame = false;

        var req = new StopFrameSyncReq();
        req.RoomId = _sessionDic["roomId"].ToString();
        req.PlayerId = _userInfo.uid;

        _socket2.SendRequest(ClientSendServerReqCmd.ECmdStopFrameSyncReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("StopFrameSyncReq");
        });
    }

    private void HandleStopFrameSync()
    {
        Record("StopFrameSyncRsp");
    }

    private void SendFrame()
    {
        var req = new SendFrameReq();
        req.RoomId = _sessionDic["roomId"].ToString();
        req.Item = GetFrameItem();

        _socket2.SendRequest(ClientSendServerReqCmd.ECmdRelaySendFrameReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("SendFrameSyncReq");
            else
                Go();
        });
    }

    private void HandleBroadcastFrameSync()
    {
        Record("BroadcastFrameSync");
    }

    private void LeaveRoom()
    {
        var req = new LeaveRoomReq();
        req.PlayerId = _userInfo.uid;
        req.RoomId = _sessionDic["roomId"].ToString();

        _socket1.SendRequest(ClientSendServerReqCmd.ECmdQuitRoomReq, _userInfo.uid, req, (ok) => {
            if (ok)
                Record("LeaveRoomReq");
        });
    }

    private void HandleLeaveRoom()
    {
        Record("LeaveRoomRsp");
        ConsoleLogger.Info(string.Format("Leave room. uid = {0}", _userInfo.uid));
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
                //ConsoleLogger.Warn(string.Format("Enter room success. response = {0}", result.Body));
                HandleEnterRoom();
                break;
            case ClientSendServerReqCmd.ECmdRoomChatReq:
                HandleGetItems();
                //ConsoleLogger.Info(string.Format("Get items success. response = {0}\n", result.Body));
                break;
            case ClientSendServerReqCmd.ECmdQuitRoomReq:
                //ConsoleLogger.Warn(string.Format("Leave room success. response = {0}", result.Body));
                HandleLeaveRoom();
                break;
        }
    }

    private void HandleFrameResponse(ClientSendServerReqCmd cmd, DecodeRspResult result)
    {
        switch (cmd)
        {
            case ClientSendServerReqCmd.ECmdStartFrameSyncReq:
                HandleStartFrameSync();
                //ConsoleLogger.Warn(string.Format("Start frame sync success. response = {0}", result.Body));
                break;
            case ClientSendServerReqCmd.ECmdStopFrameSyncReq:
                HandleStopFrameSync();
                //ConsoleLogger.Warn(string.Format("Stop frame sync success. response = {0}", result.Body));
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
                HandleBroadcastFrameSync();
                //ConsoleLogger.Warn(string.Format("Receive frame sync broadcast. response = {0}", result.Body));
                break;
        }
    }
    #endregion

    #region 数据分析
    public Dictionary<string, int> Export()
    {
        return _analysisDic;
    }

    private void Record(string type, int val = 1)
    {
        lock(_analysisDic)
        {
            if (!_analysisDic.ContainsKey(type))
                _analysisDic.Add(type, 0);

            _analysisDic[type] = _analysisDic[type] + val;
        }
    }
    #endregion
}