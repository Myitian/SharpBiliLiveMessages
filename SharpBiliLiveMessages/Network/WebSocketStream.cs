using System.Buffers;
using System.Net.WebSockets;

namespace SharpBiliLiveMessages.Network
{
    public class WebSocketStream(WebSocket ws, bool leaveOpen = false) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override bool CanTimeout => false;
        public override int ReadTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int WriteTimeout { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Close()
        {
            if (!leaveOpen)
                ws.Dispose();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (ws.State > WebSocketState.CloseReceived)
                return 0;
            while (true)
            {
                if (ws.State > WebSocketState.CloseReceived)
                    return -1;
                WebSocketReceiveResult result = ws.ReceiveAsync(new ArraySegment<byte>(buffer, offset, count), CancellationToken.None).Result;
                if (result.MessageType == WebSocketMessageType.Close)
                    return -1;
                if (result.Count != 0)
                    return buffer[0];
            }
        }

        public override int Read(Span<byte> buffer)
        {
            if (ws.State > WebSocketState.CloseReceived)
                return 0;
            using ArrayPoolWrapper<byte> array = new(buffer.Length);
            int count = Read(array.UnderlyingBuffer, 0, array.Length);
            array.Span.CopyTo(buffer);
            return count;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (ws.State > WebSocketState.CloseReceived)
                return 0;
            return await ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                if (ws.State > WebSocketState.CloseReceived)
                    return 0;
                ValueWebSocketReceiveResult result = await ws.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    return 0;
                if (result.Count != 0)
                    return result.Count;
            }
        }

        public override int ReadByte()
        {
            byte[] buffer = new byte[1];
            while (true)
            {
                if (ws.State > WebSocketState.CloseReceived)
                    return -1;
                WebSocketReceiveResult result = ws.ReceiveAsync(buffer, CancellationToken.None).Result;
                if (result.MessageType == WebSocketMessageType.Close)
                    return -1;
                if (result.Count != 0)
                    return buffer[0];
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (ws.State > WebSocketState.CloseSent)
                throw new InvalidOperationException();
            ws.SendAsync(new(buffer, offset, count), WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (ws.State > WebSocketState.CloseSent)
                throw new InvalidOperationException();
            byte[] arrayBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                ws.SendAsync(new(arrayBuffer, 0, buffer.Length), WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(arrayBuffer);
            }
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (ws.State > WebSocketState.CloseSent)
                throw new InvalidOperationException();
            return ws.SendAsync(new(buffer, offset, count), WebSocketMessageType.Binary, true, cancellationToken);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (ws.State > WebSocketState.CloseSent)
                throw new InvalidOperationException();
            return ws.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
        }

        public override void WriteByte(byte value)
        {
            if (ws.State > WebSocketState.CloseSent)
                throw new InvalidOperationException();
            byte[] buffer = [value];
            ws.SendAsync(buffer, WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
        }
    }
}
