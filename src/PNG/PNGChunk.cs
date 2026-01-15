using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;

namespace ResourcePackRepairer.PNG;

public struct PNGChunk(byte[] array) : IDisposable
{
    public bool FromPool = false;
    public readonly byte[] UnderlyingArray = array;
    public int Length;
    public uint Name;
    public readonly Span<byte> Data => UnderlyingArray.AsSpan(0, Length);
    public uint CRC32;

    public static PNGChunk Allocate(int length)
    {
        return new(length == 0 ? [] : GC.AllocateUninitializedArray<byte>(length))
        {
            Length = length,
            FromPool = false
        };
    }
    public static PNGChunk RentFromArrayPool(int length)
    {
        return new(length == 0 ? [] : ArrayPool<byte>.Shared.Rent(length))
        {
            Length = length,
            FromPool = length != 0
        };
    }
    public void ReCalculateCRC32()
    {
        Crc32 crc32 = new();
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, Name);
        crc32.Append(buffer);
        crc32.Append(Data);
        CRC32 = crc32.GetCurrentHashAsUInt32();
    }
    public readonly void WriteToStream(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteInt32BigEndian(buffer, Length);
        BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], Name);
        stream.Write(buffer);
        stream.Write(UnderlyingArray, 0, Length);
        BinaryPrimitives.WriteUInt32BigEndian(buffer, CRC32);
        stream.Write(buffer[..4]);
    }
    public static bool TryReadFromStream(Stream stream, out PNGChunk chunk)
    {
        chunk = default;
        Span<byte> buffer = stackalloc byte[8];
        if (!stream.TryReadExactly(buffer))
            return false;
        int length = BinaryPrimitives.ReadInt32BigEndian(buffer);
        chunk = RentFromArrayPool(length);
        chunk.Name = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]);
        if (!stream.TryReadExactly(chunk.Data))
            return false;
        if (!stream.TryReadExactly(buffer[..4]))
            return false;
        chunk.CRC32 = BinaryPrimitives.ReadUInt32BigEndian(buffer);
        return true;
    }

    public void Dispose()
    {
        if (FromPool)
        {
            ArrayPool<byte>.Shared.Return(UnderlyingArray);
            FromPool = false;
        }
    }
}