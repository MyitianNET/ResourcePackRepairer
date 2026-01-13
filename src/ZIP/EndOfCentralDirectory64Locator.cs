using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EndOfCentralDirectory64Locator : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x6\x7"u8;
    public uint DiskNumber;
    public ulong RelativeOffset;
    public uint TotalDisks;

    public void ReverseEndianness()
    {
        DiskNumber = BinaryPrimitives.ReverseEndianness(DiskNumber);
        RelativeOffset = BinaryPrimitives.ReverseEndianness(RelativeOffset);
        TotalDisks = BinaryPrimitives.ReverseEndianness(TotalDisks);
    }
    public override readonly string ToString()
    {
        return $"""
            DiskNumber    : {DiskNumber}
            RelativeOffset: {RelativeOffset}
            TotalDisks    : {TotalDisks}
            """;
    }
}