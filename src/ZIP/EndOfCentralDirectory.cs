using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EndOfCentralDirectory : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x5\x6"u8;
    public ushort DiskNumber;
    public ushort StartDiskNumber;
    public ushort EntriesOnThisDisk;
    public ushort TotalEntries;
    public uint DirectorySize;
    public uint DirectoryOffset;
    public ushort CommentLength;
    public void ReverseEndianness()
    {
        DiskNumber = BinaryPrimitives.ReverseEndianness(DiskNumber);
        StartDiskNumber = BinaryPrimitives.ReverseEndianness(StartDiskNumber);
        EntriesOnThisDisk = BinaryPrimitives.ReverseEndianness(EntriesOnThisDisk);
        TotalEntries = BinaryPrimitives.ReverseEndianness(TotalEntries);
        DirectorySize = BinaryPrimitives.ReverseEndianness(DirectorySize);
        DirectoryOffset = BinaryPrimitives.ReverseEndianness(DirectoryOffset);
        CommentLength = BinaryPrimitives.ReverseEndianness(CommentLength);
    }
    public override readonly string ToString()
    {
        return $"""
            DiskNumber       : {DiskNumber}
            StartDiskNumber  : {StartDiskNumber}
            EntriesOnThisDisk: {EntriesOnThisDisk}
            TotalEntries     : {TotalEntries}
            DirectorySize    : {DirectorySize}
            DirectoryOffset  : {DirectoryOffset}
            CommentLength    : {CommentLength}
            """;
    }
    public static bool FindFromStream(Stream stream, out EndOfCentralDirectory header)
    {
        stream.Seek(0, SeekOrigin.End);
        while (stream.ReadBackwardsUntilFind4ByteSeq(Signature))
        {
            long pos = stream.Position;
            if (IDataStruct.TryReadFromStream(stream, out header)
                && stream.Position + header.CommentLength <= stream.Length)
                return true;
            stream.Position = pos - 1;
        }
        header = default;
        return false;
    }
}