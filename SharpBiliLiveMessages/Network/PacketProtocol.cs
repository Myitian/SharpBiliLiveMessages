namespace SharpBiliLiveMessages.Network;

public enum PacketProtocol : ushort
{
    UncompressedNormal = 0,
    HeartbeatOrVerification = 1,
    ZlibNormal = 2,
    BrotliNormal = 3
}
