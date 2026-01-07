namespace ResourcePackRepairer.ZIP;

public class LengthLimitedStream(Stream stream, long length, bool leaveOpen = true) : Stream
{
    protected long _remaining = length;
    protected readonly bool _leaveOpen = leaveOpen;
    public Stream BaseStream { get; } = stream;
    public override bool CanRead => BaseStream.CanRead;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Close()
    {
        if (!_leaveOpen)
            BaseStream.Close();
    }
    public override void Flush()
    {
        BaseStream.Flush();
    }
    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return BaseStream.FlushAsync(cancellationToken);
    }
    public override int Read(byte[] buffer, int offset, int count)
    {
        long c = Math.Min(count, _remaining);
        int result = BaseStream.Read(buffer, offset, (int)c);
        _remaining -= c;
        return result;
    }
    public override int Read(Span<byte> buffer)
    {
        long c = Math.Min(buffer.Length, _remaining);
        int result = BaseStream.Read(buffer[..(int)c]);
        _remaining -= c;
        return result;
    }
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        long c = Math.Min(count, _remaining);
        Task<int> result = BaseStream.ReadAsync(buffer, offset, (int)c, cancellationToken);
        _remaining -= c;
        return result;
    }
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        long c = Math.Min(buffer.Length, _remaining);
        ValueTask<int> result = BaseStream.ReadAsync(buffer[..(int)c], cancellationToken);
        _remaining -= c;
        return result;
    }
    public override int ReadByte()
    {
        if (_remaining > 0)
        {
            int result = BaseStream.ReadByte();
            _remaining--;
            return result;
        }
        return -1;
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
        throw new NotSupportedException();
    }
}