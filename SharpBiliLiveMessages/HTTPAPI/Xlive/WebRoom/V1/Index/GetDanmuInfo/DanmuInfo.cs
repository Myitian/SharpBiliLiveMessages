using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages.HTTPAPI.Xlive.WebRoom.V1.Index.GetDanmuInfo;

/// <summary>
/// https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id=
/// </summary>
public class DanmuInfo
{
    [JsonIgnore]
    public const string API = "https://api.live.bilibili.com/xlive/web-room/v1/index/getDanmuInfo?id=";

    [JsonPropertyName("code")]
    public int Code { get; set; }
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    [JsonPropertyName("ttl")]
    public int TTL { get; set; }
    [JsonPropertyName("data")]
    public Data? Data { get; set; }
}
