using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CentralDirectoryHeader : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x1\x2"u8;
    public ushort VersionMadeBy;
    public ushort VersionNeeded;
    public ushort GeneralPurposeFlag;
    public ushort CompressionMethod;
    public uint LastModified;
    public uint CRC32;
    public uint CompressedSize;
    public uint UncompressedSize;
    public ushort FileNameLength;
    public ushort ExtraFieldLength;
    public ushort CommentLength;
    public ushort StartDiskNumber;
    public ushort InternalAttrs;
    public uint ExternalAttrs;
    public uint LocalHeaderOffset;
    public void ReverseEndianness()
    {
        VersionMadeBy = BinaryPrimitives.ReverseEndianness(VersionMadeBy);
        VersionNeeded = BinaryPrimitives.ReverseEndianness(VersionNeeded);
        GeneralPurposeFlag = BinaryPrimitives.ReverseEndianness(GeneralPurposeFlag);
        CompressionMethod = BinaryPrimitives.ReverseEndianness(CompressionMethod);
        LastModified = BinaryPrimitives.ReverseEndianness(LastModified);
        CRC32 = BinaryPrimitives.ReverseEndianness(CRC32);
        CompressedSize = BinaryPrimitives.ReverseEndianness(CompressedSize);
        UncompressedSize = BinaryPrimitives.ReverseEndianness(UncompressedSize);
        FileNameLength = BinaryPrimitives.ReverseEndianness(FileNameLength);
        ExtraFieldLength = BinaryPrimitives.ReverseEndianness(ExtraFieldLength);
        CommentLength = BinaryPrimitives.ReverseEndianness(CommentLength);
        StartDiskNumber = BinaryPrimitives.ReverseEndianness(StartDiskNumber);
        InternalAttrs = BinaryPrimitives.ReverseEndianness(InternalAttrs);
        ExternalAttrs = BinaryPrimitives.ReverseEndianness(ExternalAttrs);
        LocalHeaderOffset = BinaryPrimitives.ReverseEndianness(LocalHeaderOffset);
    }
    public override readonly string ToString()
    {
        return $"""
            VersionMadeBy     : {VersionMadeBy}
            VersionNeeded     : {VersionNeeded}
            GeneralPurposeFlag: {GeneralPurposeFlag}
            CompressionMethod : {CompressionMethod}
            LastModified      : {LastModified}
            CRC32             : {CRC32}
            CompressedSize    : {CompressedSize}
            UncompressedSize  : {UncompressedSize}
            FileNameLength    : {FileNameLength}
            ExtraFieldLength  : {ExtraFieldLength}
            CommentLength     : {CommentLength}
            DiskNumberStart   : {StartDiskNumber}
            InternalAttrs     : {InternalAttrs}
            ExternalAttrs     : {ExternalAttrs}
            LocalHeaderOffset : {LocalHeaderOffset}
            """;
    }
}