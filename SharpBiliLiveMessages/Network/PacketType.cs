namespace SharpBiliLiveMessages.Network;

public enum PacketType : uint
{
    Heartbeat = 2,
    HeartbeatResponse = 3,
    Normal = 5,
    Verification = 7,
    VerificationResponse = 8
}
