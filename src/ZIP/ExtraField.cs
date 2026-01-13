using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace ResourcePackRepairer.ZIP;

public struct ExtraField
{
    public ushort ID;
    public byte[]? Data;

    [MemberNotNullWhen(true, nameof(Data))]
    public readonly bool IsValid => Data?.Length is >= 0 and <= ushort.MaxValue;

    public static int TryReadFromBytes(ReadOnlySpan<byte> bytes, out ExtraField result)
    {
        const int HeadSize = sizeof(ushort) * 2;
        result = default;
        if (bytes.Length < HeadSize)
            return 0;
        int end = BinaryPrimitives.ReadUInt16LittleEndian(bytes[sizeof(ushort)..]) + HeadSize;
        if (bytes.Length < end)
            return 0;
        result.ID = BinaryPrimitives.ReadUInt16LittleEndian(bytes);
        result.Data = bytes[HeadSize..end].ToArray();
        return end;
    }
    public readonly void WriteToStream(Stream stream)
    {
        const int HeadSize = sizeof(ushort) * 2;
        if (!IsValid)
            throw new NotSupportedException("Invalid data size");
        Span<byte> buffer = stackalloc byte[HeadSize];
        BinaryPrimitives.WriteUInt16LittleEndian(buffer, ID);
        BinaryPrimitives.WriteUInt16LittleEndian(buffer[sizeof(ushort)..], (ushort)Data.Length);
        stream.Write(buffer);
        stream.Write(Data, 0, Data.Length);
    }
}