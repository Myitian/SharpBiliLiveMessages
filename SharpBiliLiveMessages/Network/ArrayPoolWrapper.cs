using System.Buffers;

namespace SharpBiliLiveMessages.Network;

public class ArrayPoolWrapper<T> : IDisposable
{
    private bool disposed = false;

    public T[] UnderlyingBuffer { get; }
    public Memory<T> Memory { get; }
    public int Length { get; }
    public Span<T> Span => Memory.Span;

    public ArrayPoolWrapper(int length)
    {
        UnderlyingBuffer = ArrayPool<T>.Shared.Rent(length);
        Memory = UnderlyingBuffer.AsMemory(0, length);
        Length = length;
    }

    public static implicit operator Span<T>(ArrayPoolWrapper<T> buffer)
        => buffer.Span;
    public static implicit operator ReadOnlySpan<T>(ArrayPoolWrapper<T> buffer)
        => buffer.Span;
    public static implicit operator Memory<T>(ArrayPoolWrapper<T> buffer)
        => buffer.Memory;
    public static implicit operator ReadOnlyMemory<T>(ArrayPoolWrapper<T> buffer)
        => buffer.Memory;

    public void Dispose()
    {
        if (!disposed)
        {
            if (UnderlyingBuffer is not null)
                ArrayPool<T>.Shared.Return(UnderlyingBuffer);
            disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
