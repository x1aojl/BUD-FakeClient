public class MapInfo
{
    public string mapId;
    public string version;
    public int isPrivate;
    public string roomCode;
    public int maxPlayerCount;
    public string isPvp;
    public string roomType { get { return string.Format("{0}|{1}", mapId, version); } }
}