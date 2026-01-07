using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LocalFileHeader(CentralDirectoryHeader cdh) : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x3\x4"u8;
    public ushort VersionNeeded = cdh.VersionNeeded;
    public ushort GeneralPurposeFlag = cdh.GeneralPurposeFlag;
    public ushort CompressionMethod = cdh.CompressionMethod;
    public uint LastModified = cdh.LastModified;
    public uint CRC32 = cdh.CRC32;
    public uint CompressedSize = cdh.CompressedSize;
    public uint UncompressedSize = cdh.UncompressedSize;
    public ushort FileNameLength = cdh.FileNameLength;
    public ushort ExtraFieldLength = cdh.ExtraFieldLength;
    public void ReverseEndianness()
    {
        VersionNeeded = BinaryPrimitives.ReverseEndianness(VersionNeeded);
        GeneralPurposeFlag = BinaryPrimitives.ReverseEndianness(GeneralPurposeFlag);
        CompressionMethod = BinaryPrimitives.ReverseEndianness(CompressionMethod);
        LastModified = BinaryPrimitives.ReverseEndianness(LastModified);
        CRC32 = BinaryPrimitives.ReverseEndianness(CRC32);
        CompressedSize = BinaryPrimitives.ReverseEndianness(CompressedSize);
        UncompressedSize = BinaryPrimitives.ReverseEndianness(UncompressedSize);
        FileNameLength = BinaryPrimitives.ReverseEndianness(FileNameLength);
        ExtraFieldLength = BinaryPrimitives.ReverseEndianness(ExtraFieldLength);
    }
    public override readonly string ToString()
    {
        return $"""
            VersionNeeded     : {VersionNeeded}
            GeneralPurposeFlag: {GeneralPurposeFlag}
            CompressionMethod : {CompressionMethod}
            LastModified      : {LastModified}
            CRC32             : {CRC32}
            CompressedSize    : {CompressedSize}
            UncompressedSize  : {UncompressedSize}
            FileNameLength    : {FileNameLength}
            ExtraFieldLength  : {ExtraFieldLength}
            """;
    }
}