using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EndOfCentralDirectory64(EndOfCentralDirectory eocd) : IDataStruct
{
    public static ReadOnlySpan<byte> Signature => "PK\x6\x6"u8;
    public static readonly uint MaxAllowedSize = (uint)Unsafe.SizeOf<EndOfCentralDirectory64>() + (uint)Array.MaxLength - sizeof(ulong);
    public ulong SizeOfRecord = 0;
    public ushort VersionMadeBy;
    public ushort VersionNeeded;
    public uint DiskNumber = eocd.DiskNumber;
    public uint StartDiskNumber = eocd.StartDiskNumber;
    public ulong EntriesOnThisDisk = eocd.EntriesOnThisDisk;
    public ulong TotalEntries = eocd.TotalEntries;
    public ulong DirectorySize = eocd.DirectorySize;
    public ulong DirectoryOffset = eocd.DirectoryOffset;
    public readonly int SizeOfExtras => (int)((uint)SizeOfRecord - (uint)Unsafe.SizeOf<EndOfCentralDirectory64>() + sizeof(ulong));

    public void ReverseEndianness()
    {
        SizeOfRecord = BinaryPrimitives.ReverseEndianness(SizeOfRecord);
        VersionMadeBy = BinaryPrimitives.ReverseEndianness(VersionMadeBy);
        VersionNeeded = BinaryPrimitives.ReverseEndianness(VersionNeeded);
        DiskNumber = BinaryPrimitives.ReverseEndianness(DiskNumber);
        StartDiskNumber = BinaryPrimitives.ReverseEndianness(StartDiskNumber);
        EntriesOnThisDisk = BinaryPrimitives.ReverseEndianness(EntriesOnThisDisk);
        TotalEntries = BinaryPrimitives.ReverseEndianness(TotalEntries);
        DirectorySize = BinaryPrimitives.ReverseEndianness(DirectorySize);
        DirectoryOffset = BinaryPrimitives.ReverseEndianness(DirectoryOffset);
    }
    public override readonly string ToString()
    {
        return $"""
            SizeOfRecord     : {SizeOfRecord}
            VersionMadeBy    : {VersionMadeBy}
            VersionNeeded    : {VersionNeeded}
            DiskNumber       : {DiskNumber}
            StartDiskNumber  : {StartDiskNumber}
            EntriesOnThisDisk: {EntriesOnThisDisk}
            TotalEntries     : {TotalEntries}
            DirectorySize    : {DirectorySize}
            DirectoryOffset  : {DirectoryOffset}
            """;
    }
}