using System.Buffers;
using System.Text;

namespace SharpBiliLiveMessages.Network;

public sealed class PacketSender(Stream stream)
{
    private readonly object lockObject = new();
    private PacketHeader header;

    public uint Sequence { get; private set; } = 0;

    public void Send(PacketProtocol protocol, PacketType type, ReadOnlySpan<byte> body, bool sendTogether = false)
    {
        lock (lockObject)
        {
            header.ContentLength = (uint)body.Length;
            header.Protocol = protocol;
            header.Type = type;
            header.Sequence = Sequence++;

            if (sendTogether)
            {
                int len = 16;
                checked
                {
                    len += body.Length;
                }
                byte[]? array = null;
                Span<byte> buffer = len <= Utils.MaxStackAlloc ?
                    stackalloc byte[len] :
                    array = ArrayPool<byte>.Shared.Rent(len);

                try
                {
                    header.WriteToSpan(buffer);
                    body.CopyTo(buffer[16..]);
                    stream.Write(buffer[..len]);
                }
                finally
                {
                    if (array is not null)
                        ArrayPool<byte>.Shared.Return(array);
                }
            }
            else
            {
                header.WriteToStream(stream);// Console.WriteLine(Encoding.UTF8.GetString(body));
                stream.Write(body);
            }
        }
    }
}
