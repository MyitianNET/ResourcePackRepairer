using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer;

internal static class StreamHelpers
{
    // position after returned:
    //                   v
    // [*][*][0][1][2][3][*][*]
    public static bool ReadBackwardsUntilFind4ByteSeq(this Stream stream, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 4)
            throw new ArgumentException("bytes.Length must be 4!");
        uint value = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
        uint current = 0;
        long pos = stream.Position;
        while (pos <= 4)
            return false;
        stream.Position = pos -= 4;
        if (!stream.TryReadExactly(MemoryMarshal.AsBytes(new Span<uint>(ref current))))
            return false;
        while (current != value)
        {
            if (--pos < 0)
                return false;
            stream.Position = pos;
            int read = stream.ReadByte();
            if (read < 0)
                return false;
            current = (current << 8) | (uint)read;
        }
        stream.Position = pos + 4;
        return true;
    }
    // position after returned:
    //                   v
    // [*][*][0][1][2][3][*][*]
    public static bool ReadForwardsUntilFind4ByteSeq(this Stream stream, ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 4)
            throw new ArgumentException("bytes.Length must be 4!");
        uint value = BinaryPrimitives.ReadUInt32BigEndian(bytes);
        uint current = 0;
        if (!stream.TryReadExactly(MemoryMarshal.AsBytes(new Span<uint>(ref current))))
            return false;
        if (BitConverter.IsLittleEndian)
            current = BinaryPrimitives.ReverseEndianness(current);
        while (current != value)
        {
            int read = stream.ReadByte();
            if (read < 0)
                return false;
            current = (current << 8) | (uint)read;
        }
        return true;
    }
    // position after returned:
    //                      v
    // [*][*][0][1][2]...[N][*][*]
    public static bool StartsWith(this Stream stream, ReadOnlySpan<byte> bytes)
    {
        byte[] array = ArrayPool<byte>.Shared.Rent(bytes.Length);
        try
        {
            Span<byte> buffer = array.AsSpan(0, bytes.Length);
            return stream.ReadAtLeast(buffer, buffer.Length, false) == buffer.Length
                && buffer.SequenceEqual(bytes);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public static bool TryReadExactly(this Stream stream, Span<byte> buffer)
    {
        return stream.ReadAtLeast(buffer, buffer.Length, false) == buffer.Length;
    }
    public static byte[] ReadBytes(this Stream stream, int length)
    {
        byte[] bytes = GC.AllocateUninitializedArray<byte>(length);
        stream.ReadExactly(bytes);
        return bytes;
    }
    public static void LengthCopy(this Stream stream, Stream destination, ulong length)
    {
        if (length == 0)
            return;
        byte[] array = ArrayPool<byte>.Shared.Rent((int)Math.Min(length, 65536));
        ulong arrayLength = (ulong)array.Length;
        try
        {
            while (length > arrayLength)
            {
                int read = stream.Read(array, 0, array.Length);
                if (read == 0)
                    throw new EndOfStreamException();
                destination.Write(array, 0, read);
                length -= (ulong)read;
            }
            int iLength = (int)length;
            int finalBlock = stream.Read(array, 0, iLength);
            destination.Write(array, 0, finalBlock);
            if (finalBlock < iLength)
                throw new EndOfStreamException();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }
}