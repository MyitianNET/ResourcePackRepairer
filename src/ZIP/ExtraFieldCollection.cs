using System.Buffers.Binary;

namespace ResourcePackRepairer.ZIP;

public sealed class ExtraFieldCollection()
{
    public readonly List<ExtraField> ExtraFields = [];
    public byte[] RemainingData = [];

    public ExtraFieldCollection(ReadOnlySpan<byte> bytes) : this()
    {
        ReadFromBytes(bytes);
    }
    public bool TryGetLengthInBytes(out ushort length)
    {
        const int HeadSize = sizeof(ushort) * 2;
        length = 0;
        int len = RemainingData.Length;
        foreach (ExtraField extraField in ExtraFields)
        {
            if (!extraField.IsValid)
                return false;
            len += extraField.Data.Length + HeadSize;
            if (len > ushort.MaxValue)
                return false;
        }
        length = (ushort)len;
        return true;
    }
    public void ReadFromBytes(ReadOnlySpan<byte> bytes)
    {
        ExtraFields.Clear();
        while (true)
        {
            int read = ExtraField.TryReadFromBytes(bytes, out ExtraField extraField);
            if (read == 0)
            {
                RemainingData = bytes.ToArray();
                break;
            }
            ExtraFields.Add(extraField);
            bytes = bytes[read..];
        }
    }
    public void WriteToStream(Stream stream)
    {
        foreach (ExtraField extraField in ExtraFields)
            extraField.WriteToStream(stream);
        stream.Write(RemainingData, 0, RemainingData.Length);
    }
    public void ReadZip64ExtraField(
        uint uncompressedSize, uint compressedSize, uint localHeaderOffset, ushort startDiskNumber,
        out ulong uncompressedSize64, out ulong compressedSize64, out ulong localHeaderOffset64, out uint startDiskNumber32)
    {
        foreach (ExtraField extraField in ExtraFields)
        {
            if (extraField.ID == 0x0001)
            {
                ReadOnlySpan<byte> data = extraField.Data;
                if (uncompressedSize != uint.MaxValue)
                    uncompressedSize64 = uncompressedSize;
                else
                {
                    uncompressedSize64 = BinaryPrimitives.ReadUInt64LittleEndian(data);
                    data = data[sizeof(ulong)..];
                }
                if (compressedSize != uint.MaxValue)
                    compressedSize64 = compressedSize;
                else
                {
                    compressedSize64 = BinaryPrimitives.ReadUInt64LittleEndian(data);
                    data = data[sizeof(ulong)..];
                }
                if (localHeaderOffset != uint.MaxValue)
                    localHeaderOffset64 = localHeaderOffset;
                else
                {
                    localHeaderOffset64 = BinaryPrimitives.ReadUInt64LittleEndian(data);
                    data = data[sizeof(ulong)..];
                }
                if (startDiskNumber != ushort.MaxValue)
                    startDiskNumber32 = startDiskNumber;
                else
                    startDiskNumber32 = BinaryPrimitives.ReadUInt32LittleEndian(data);
                return;
            }
        }
        // uncompressedSize and startDiskNumber is not necessary for this tool
        if ((compressedSize & localHeaderOffset) == uint.MaxValue)
            throw new InvalidDataException();
        uncompressedSize64 = uncompressedSize;
        compressedSize64 = compressedSize;
        localHeaderOffset64 = localHeaderOffset;
        startDiskNumber32 = startDiskNumber;
    }
}