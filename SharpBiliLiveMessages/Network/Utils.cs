namespace SharpBiliLiveMessages.Network;

public static class Utils
{
    public const int MaxStackAlloc = 3072;

    public static void Skip(this Stream stream, long offset, bool throwOnEndOfStream = true)
    {
        if (offset <= 0)
            return;
        else if (offset <= MaxStackAlloc)
        {
            int iOffset = (int)offset;
            Span<byte> buffer = stackalloc byte[iOffset];
            stream.ReadAtLeast(buffer, iOffset, throwOnEndOfStream);
        }
        else
        {
            Span<byte> buffer = stackalloc byte[MaxStackAlloc];
            while (offset >= MaxStackAlloc)
            {
                int read = stream.Read(buffer);
                if (read == 0 && throwOnEndOfStream)
                    throw new EndOfStreamException();
                offset -= read;
            }
            int iOffset = (int)offset;
            stream.ReadAtLeast(buffer[..iOffset], iOffset, throwOnEndOfStream);
        }
    }
}
