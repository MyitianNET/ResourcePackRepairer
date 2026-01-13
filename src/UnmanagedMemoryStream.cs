using System.Runtime.InteropServices;

namespace ResourcePackRepairer;

internal sealed unsafe class SimpleUnmanagedMemoryStream : Stream
{
    private byte* _pointer;
    private long _length;
    public override bool CanRead => _length >= 0;
    public override bool CanSeek => _length >= 0;
    public override bool CanWrite => _length >= 0;
    public override long Length => _length;
    public override long Position
    {
        get;
        set
        {
            ObjectDisposedException.ThrowIf(_length < 0, this);
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    } = 0;

    public SimpleUnmanagedMemoryStream()
    {
        _pointer = null;
        _length = 0;
    }

    public SimpleUnmanagedMemoryStream(long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((ulong)length, nuint.MaxValue, nameof(length));
        _pointer = (byte*)NativeMemory.Alloc((nuint)(ulong)length);
        _length = length;
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        switch (origin)
        {
            case SeekOrigin.Begin:
                ArgumentOutOfRangeException.ThrowIfNegative(offset);
                Position = offset;
                break;
            case SeekOrigin.Current:
                ArgumentOutOfRangeException.ThrowIfNegative(offset + Position);
                Position += offset;
                break;
            case SeekOrigin.End:
                ArgumentOutOfRangeException.ThrowIfNegative(offset + Length);
                Position = offset + Length;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }
        return Position;
    }

    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        nuint valueN = checked((nuint)value);
        _pointer = (byte*)NativeMemory.Realloc(_pointer, valueN);
        _length = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<int>(cancellationToken);

        try
        {
            int n = Read(buffer, offset, count);
            return Task.FromResult(n);
        }
        catch (Exception ex)
        {
            return Task.FromException<int>(ex);
        }
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled<int>(cancellationToken);

        try
        {
            int n = Read(buffer.Span);
            return ValueTask.FromResult(n);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<int>(ex);
        }
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        long pos = Position;
        long len = Length;
        if (pos >= len)
            return 0;
        uint maxRead = (uint)Math.Min(len - pos, buffer.Length);
        byte* start = _pointer + (nuint)(ulong)pos;
        fixed (byte* buf = buffer)
            NativeMemory.Copy(start, buf, maxRead);
        Position = pos + maxRead;
        return (int)maxRead;
    }

    public override int ReadByte()
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        long pos = Position;
        long len = Length;
        if (pos >= len)
            return -1;
        byte value = _pointer[(ulong)pos];
        Position = pos + 1;
        return value;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        Write(buffer.AsSpan(offset, count));
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);

        try
        {
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            return Task.FromException(ex);
        }
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return ValueTask.FromCanceled(cancellationToken);

        try
        {
            Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
        catch (Exception ex)
        {
            return ValueTask.FromException(ex);
        }
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        long pos = Position;
        long newPosition = pos + buffer.Length;
        EnsureCapacity(newPosition);
        byte* start = _pointer + (nuint)Position;
        fixed (byte* buf = buffer)
            NativeMemory.Copy(buf, start, (uint)buffer.Length);
        Position = newPosition;
    }

    public override void WriteByte(byte value)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        long pos = Position;
        long newPosition = pos + 1;
        EnsureCapacity(newPosition);
        _pointer[(ulong)pos] = value;
        Position = newPosition;
    }

    public void EnsureCapacity(long minNewSize)
    {
        ObjectDisposedException.ThrowIf(_length < 0, this);
        nuint minNewSizeN = checked((nuint)minNewSize);
        if (minNewSize > _length)
        {
            _pointer = (byte*)NativeMemory.Realloc(_pointer, minNewSizeN);
            _length = minNewSize;
        }
    }

    protected override void Dispose(bool disposing)
    {
        NativeMemory.Free(_pointer);
        _pointer = null;
        _length = -1;
        base.Dispose(disposing);
    }

    ~SimpleUnmanagedMemoryStream()
    {
        Dispose(false);
    }
}