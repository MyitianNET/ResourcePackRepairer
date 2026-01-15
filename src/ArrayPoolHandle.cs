using System.Buffers;

namespace ResourcePackRepairer;

struct PooledArrayHandle<T>(int minimumLength) : IDisposable
{
    public T[] Array = ArrayPool<T>.Shared.Rent(minimumLength);
    public void Dispose()
    {
        if (Array is not null)
        {
            ArrayPool<T>.Shared.Return(Array);
            Array = null!;
        }
    }
}
