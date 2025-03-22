namespace SharpBiliLiveMessages.Network;

public struct PacketHeader
{
    public uint ContentLength;
    public PacketProtocol Protocol;
    public PacketType Type;
    public uint Sequence;

    public bool ReadFromStream(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[16];
        if (stream.ReadAtLeast(buffer, buffer.Length, false) < buffer.Length)
            return false;
        ContentLength = (uint)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]);
        ushort headerSize = (ushort)((buffer[4] << 8) | buffer[5]);
        ContentLength -= headerSize;
        Protocol = (PacketProtocol)((buffer[6] << 8) | buffer[7]);
        Type = (PacketType)((buffer[8] << 24) | (buffer[9] << 16) | (buffer[10] << 8) | buffer[11]);
        Sequence = (uint)((buffer[12] << 24) | (buffer[13] << 16) | (buffer[14] << 8) | buffer[15]);
        stream.Skip(16 - headerSize);
        return true;
    }

    public readonly void WriteToStream(Stream stream)
    {
        uint length = ContentLength + 16;
        ushort protocol = (ushort)Protocol;
        uint type = (uint)Type;
        ReadOnlySpan<byte> buffer = [
            (byte)(length >> 24),
            (byte)(length >> 16),
            (byte)(length >> 8),
            (byte)length,
            0x00,
            0x10,
            (byte)(protocol >> 8),
            (byte)protocol,
            (byte)(type >> 24),
            (byte)(type >> 16),
            (byte)(type >> 8),
            (byte)type,
            (byte)(Sequence >> 24),
            (byte)(Sequence >> 16),
            (byte)(Sequence >> 8),
            (byte)Sequence,
        ];
        stream.Write(buffer);
    }

    public readonly void WriteToSpan(Span<byte> buffer)
    {
        uint length = ContentLength + 16;
        ushort protocol = (ushort)Protocol;
        uint type = (uint)Type;
        buffer[0x0] = (byte)(length >> 24);
        buffer[0x1] = (byte)(length >> 16);
        buffer[0x2] = (byte)(length >> 8);
        buffer[0x3] = (byte)length;
        buffer[0x4] = 0x00;
        buffer[0x5] = 0x10;
        buffer[0x6] = (byte)(protocol >> 8);
        buffer[0x7] = (byte)protocol;
        buffer[0x8] = (byte)(type >> 24);
        buffer[0x9] = (byte)(type >> 16);
        buffer[0xA] = (byte)(type >> 8);
        buffer[0xB] = (byte)type;
        buffer[0xC] = (byte)(Sequence >> 24);
        buffer[0xD] = (byte)(Sequence >> 16);
        buffer[0xE] = (byte)(Sequence >> 8);
        buffer[0xF] = (byte)Sequence;
    }
}
