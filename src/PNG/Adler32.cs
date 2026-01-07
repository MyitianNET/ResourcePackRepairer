namespace ResourcePackRepairer.PNG;

public class Adler32
{
    public const uint ModAdler = 65521;
    private uint _s1 = 1;
    private uint _s2 = 0;

    public void Append(ReadOnlySpan<byte> data)
    {
        int length = data.Length;
        int offset = 0;
        while (length > 0)
        {
            int step = Math.Min(length, 5552);
            length -= step;
            for (int i = 0; i < step; i++)
            {
                _s1 += data[offset++];
                _s2 += _s1;
            }
            _s1 %= ModAdler;
            _s2 %= ModAdler;
        }
    }

    public uint GetCurrentHashAsUInt32()
    {
        return (_s2 << 16) | _s1;
    }

    public void Reset()
    {
        _s1 = 1;
        _s2 = 0;
    }
}