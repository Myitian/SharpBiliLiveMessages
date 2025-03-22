using SharpBiliLiveMessages.HTTPAPI.Xlive.WebRoom.V1.Index.GetDanmuInfo;
using SharpBiliLiveMessages.Network.Requests;
using SharpBiliLiveMessages.Network.Responses;
using System.Text.Json.Serialization;

namespace SharpBiliLiveMessages;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DanmuInfo))]
[JsonSerializable(typeof(Verification))]
[JsonSerializable(typeof(VerificationResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}