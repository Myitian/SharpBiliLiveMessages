using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages.Network.Responses
{
    public struct VerificationResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }
    }
}