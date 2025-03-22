using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages.HTTPAPI.Xlive.WebRoom.V1.Index.GetDanmuInfo;

public class Data
{
    [JsonPropertyName("group")]
    public string? Group { get; set; }
    [JsonPropertyName("business_id")]
    public int BusinessID { get; set; }
    [JsonPropertyName("refresh_row_factor")]
    public float RefreshRowFactor { get; set; }
    [JsonPropertyName("refresh_rate")]
    public int RefreshRate { get; set; }
    [JsonPropertyName("max_delay")]
    public int MaxDelay { get; set; }
    [JsonPropertyName("token")]
    public string? Token { get; set; }
    [JsonPropertyName("host_list")]
    public List<HostInfo>? HostList { get; set; }
}
