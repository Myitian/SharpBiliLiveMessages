using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages.Network.Requests
{
    public struct Verification
    {
        public Verification() { }

        [JsonPropertyName("uid")]
        public long? UID { get; set; }
        [JsonPropertyName("roomid")]
        public long RoomID { get; set; }
        [JsonPropertyName("protover")]
        public int? ProtoVer { get; set; } = 3;
        [JsonPropertyName("platform")]
        public string? Platform { get; set; } = "web";
        [JsonPropertyName("type")]
        public int? Type { get; set; } = 2;
        [JsonPropertyName("key")]
        public string? Key { get; set; }
    }
}