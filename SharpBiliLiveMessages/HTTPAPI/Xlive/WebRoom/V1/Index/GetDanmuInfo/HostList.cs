using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages.HTTPAPI.Xlive.WebRoom.V1.Index.GetDanmuInfo;

public class HostInfo
{
    [JsonPropertyName("host")]
    public string? Host { get; set; }
    [JsonPropertyName("port")]
    public ushort Port { get; set; }
    [JsonPropertyName("wss_port")]
    public ushort WSSPort { get; set; }
    [JsonPropertyName("ws_port")]
    public ushort WSPort { get; set; }
}
