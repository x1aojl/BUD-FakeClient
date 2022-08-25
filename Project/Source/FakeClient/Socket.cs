using System;
using System.Collections.Generic;
using System.IO;
using BudEngine.NetEngine;
using BudEngine.NetEngine.src.Util;
using WebSocketSharp;

public enum MessageDataTag : byte
{
    ClientPre = 0x02,
    ClientEnd = 0x03,

    ServerPre = 0x28,
    ServerEnd = 0x29,
}

public struct MessageWrapper
{
    public byte   Pre  { get; set; }
    public byte   End  { get; set; }
    public byte[] Body { get; set; }
};

public class Socket
{
    public Action onConnect;
    public Action<ClientSendServerReqCmd, DecodeRspResult> onResponse;
    public Action<DecodeBstResult> onBroadcast;

    private WebSocket _socket;
    private Dictionary<string, int> _sendDic = new Dictionary<string, int>();

    public Socket(string ip, int port)
    {
        var url = string.Format("ws://{0}:{1}/", ip, port);

        _socket = new WebSocket(url);

        _socket.OnOpen += OnConnect;
        _socket.OnMessage += OnMessage;
    }

    public void Connect()
    {
        _socket.Connect();
    }

    public void SendRequest(ClientSendServerReqCmd cmd, string userId, Object body, Action<bool> callback)
    {
        var seq = Guid.NewGuid().ToString();
        _sendDic.Add(seq, (int)cmd);

        var req = new ClientSendServerReq();
        req.Cmd = cmd;
        req.Seq = seq;
        req.Metadata.Add("UserId", userId);

        var bytes = Pb.EncodeReq(req, (global::Google.Protobuf.IMessage)body);
        var data = Pack((byte)MessageDataTag.ClientPre, bytes, (byte)MessageDataTag.ClientEnd);

        _socket.Send(data, (ok) => {
            callback?.Invoke(ok);
        });
    }

    private void OnConnect(object sender, EventArgs e)
    {
        onConnect?.Invoke();
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        if (! e.IsBinary)
            return;

        var rawData = e.RawData;
        if (rawData.Length == 0)
            return;

        var data = Unpack(rawData);

        try
        {
            // Response
            if (data.Pre == (byte)MessageDataTag.ClientPre && data.End == (byte)MessageDataTag.ClientEnd)
            {
                var rsp = Pb.DecodeRsp(data.Body, (_seq) => {
                    return GetRequestCmd(_seq);
                });

                var seq = rsp.RspPacket.Seq;
                var cmd = (ClientSendServerReqCmd)GetRequestCmd(seq);

                _sendDic.Remove(seq);

                onResponse?.Invoke(cmd, rsp);
            }
            // Broadcast
            else if (data.Pre == (byte)MessageDataTag.ServerPre && data.End == (byte)MessageDataTag.ServerEnd)
            {
                var bst = Pb.DecodeBst(data.Body);
                onBroadcast?.Invoke(bst);
            }
        }
        catch (Exception ex)
        {
        }
    }

    private int GetRequestCmd(string seq)
    {
        return _sendDic.ContainsKey(seq) ? _sendDic[seq] : 0;
    }

    private byte[] Pack(byte pre, byte[] body, byte end)
    {
        var uintValue = (uint)(body.Length);

        var uintBytes = BitConverter.GetBytes(uintValue);
        Array.Reverse(uintBytes);

        var memory = new MemoryStream();
        var writer = new BinaryWriter(memory);

        writer.Write(pre);
        writer.Write(uintBytes);
        writer.Write(body);
        writer.Write(end);

        return memory.ToArray();
    }

    private MessageWrapper Unpack(byte[] data)
    {
        var memory = new MemoryStream(data);
        var reader = new BinaryReader(memory);

        var pre = reader.ReadByte();
        var pkgLenBytes = reader.ReadBytes(4);
        var body = reader.ReadBytes(data.Length - 6);
        var end = reader.ReadByte();

        var msg = new MessageWrapper();
        msg.Pre = pre;
        msg.Body = body;
        msg.End = end;

        return msg;
    }
}