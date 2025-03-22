namespace SharpBiliLiveMessages.Network;

public sealed class PacketReceiver
{
    private Stream stream;
    private readonly Action<PacketHeader, ArrayPoolWrapper<byte>> onReceive;
    private readonly Thread readThread;

    public PacketReceiver(Stream stream, Action<PacketHeader, ArrayPoolWrapper<byte>> onReceive)
    {
        this.stream = stream;
        this.onReceive = onReceive;
        readThread = new(Run);
        readThread.Start();
    }

    void Run()
    {
        while (true)
        {
            PacketHeader header = new();
            ArrayPoolWrapper<byte> body;
            try
            {
                header.ReadFromStream(stream);

                if (header.ContentLength > int.MaxValue)
                    throw new NotSupportedException();

                body = new((int)header.ContentLength);
                stream.ReadExactly(body);
            }
            catch (EndOfStreamException)
            {
                break;
            }
            catch (NullReferenceException)
            {
                break;
            }
            try
            {
                onReceive(header, body);
            }
            finally
            {
                body.Dispose();
            }
        }
    }

    public void Stop()
    {
        stream = null!;
        readThread.Join();
    }
    public void Wait()
    {
        readThread.Join();
    }
}
