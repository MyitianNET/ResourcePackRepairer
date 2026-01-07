using System.Buffers;
using System.IO.Compression;
using System.IO.Hashing;

namespace ResourcePackRepairer.ZIP;

public static class ZIPRepairer
{
    public static void Repair(Stream source, Stream destination)
    {
        Repair(source, destination, new());
    }
    public static void Repair(Stream source, Stream destination, Options options)
    {
        // Read EOCD from source
        if (!EndOfCentralDirectory.FindFromStream(source, out EndOfCentralDirectory eocd))
            throw new InvalidDataException("Cannot find EOCD!");
        if (options.IgnoreDiskNumber)
        {
            eocd.DiskNumber = 0;
            eocd.StartDiskNumber = 0;
        }
        else if (eocd.DiskNumber != 0 || eocd.StartDiskNumber != 0)
        {
            throw new NotSupportedException("Spanned ZIP is not supported!");
        }
        byte[] comment = source.ReadBytes(eocd.CommentLength);

        source.Position = eocd.DirectoryOffset;
        List<(CentralDirectoryHeader, byte[])> cdhs = [];
        while (source.Position < eocd.DirectoryOffset + eocd.DirectorySize)
        {
            // Read CDH from source
            long pos = source.Position;
            if (!source.StartsWith(CentralDirectoryHeader.Signature))
                throw new InvalidDataException($"Cannot find CDH signature at {pos}!");
            CentralDirectoryHeader cdh = IDataStruct.ReadExactlyFromStream<CentralDirectoryHeader>(source);
            if (options.IgnoreDiskNumber)
                cdh.StartDiskNumber = 0;
            else if (cdh.StartDiskNumber != 0)
                throw new NotSupportedException("Spanned ZIP is not supported!");
            byte[] dynLengthContent = source.ReadBytes(cdh.FileNameLength + cdh.ExtraFieldLength + cdh.CommentLength);

            // Read LFH from source
            pos = source.Position;
            source.Position = cdh.LocalHeaderOffset;
            if (!source.StartsWith(LocalFileHeader.Signature))
                throw new InvalidDataException($"Cannot find LFH signature at {pos}!");
            LocalFileHeader lfh = IDataStruct.ReadExactlyFromStream<LocalFileHeader>(source);
            long payloadStart = source.Position + lfh.FileNameLength + lfh.ExtraFieldLength;

            // Try decompress to get correct CRC32 and length
            source.Position = payloadStart;
            if (cdh.CompressionMethod is 0 or 8 && (options.ReCalculateCRC32 || options.ReCalculateUncompressedSize))
            {
                LengthLimitedStream lls = new(source, cdh.CompressedSize);
                using Stream s = cdh.CompressionMethod == 0 ? lls : new DeflateStream(lls, CompressionMode.Decompress);
                CalculateStreamCRC32(lls, out uint crc32, out ulong length);
                if (options.ReCalculateCRC32)
                    cdh.CRC32 = crc32;
                if (options.ReCalculateUncompressedSize)
                {
                    if (length <= uint.MaxValue)
                        cdh.UncompressedSize = (uint)length;
                    else
                    {
                        // TODO: ZIP-64
                        cdh.UncompressedSize = uint.MaxValue;
                    }
                }
            }

            // Save CDH to list
            cdh.LocalHeaderOffset = GetUInt32OrThrow(destination.Length, "Destination offset is too large!");
            cdhs.Add((cdh, dynLengthContent));

            // Write LFH to destination
            lfh = new(cdh);
            destination.Write(LocalFileHeader.Signature);
            IDataStruct.WriteToStream(destination, lfh);
            destination.Write(dynLengthContent, 0, lfh.FileNameLength + lfh.ExtraFieldLength);

            // Copy compressed data from source to destination
            source.Position = payloadStart;
            source.LengthCopy(destination, lfh.CompressedSize);
            source.Position = pos;
        }
        uint cdhStartPos = GetUInt32OrThrow(destination.Length, "Destination offset is too large!");
        foreach ((CentralDirectoryHeader cdh, byte[] dynLengthContent) in cdhs)
        {
            // Write CDH to destination
            destination.Write(CentralDirectoryHeader.Signature);
            IDataStruct.WriteToStream(destination, cdh);
            destination.Write(dynLengthContent, 0, dynLengthContent.Length);
        }

        // Write EOCD to destination
        eocd.DirectoryOffset = cdhStartPos;
        eocd.DirectorySize = GetUInt32OrThrow(destination.Length - cdhStartPos, "Destination offset is too large!");
        if (options.ReCalculateEntryCount)
            eocd.TotalEntries = eocd.EntriesOnThisDisk = GetUInt16OrThrow(cdhs.Count, "Too many central directory headers!");
        destination.Write(EndOfCentralDirectory.Signature);
        IDataStruct.WriteToStream(destination, eocd);
        destination.Write(comment, 0, comment.Length);

        static uint GetUInt32OrThrow(long value, string msg)
        {
            return value <= uint.MaxValue ? (uint)value
                : throw new NotSupportedException(msg);
        }
        static ushort GetUInt16OrThrow(int value, string msg)
        {
            return value <= ushort.MaxValue ? (ushort)value
                : throw new NotSupportedException(msg);
        }
    }

    public static void CalculateStreamCRC32(Stream stream, out uint crc32, out ulong length)
    {
        const int BufferSize = 65536;

        byte[] array = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            Crc32 crc = new();
            length = 0;
            while (true)
            {
                int read = stream.Read(array, 0, array.Length);
                if (read <= 0)
                    break;
                crc.Append(array.AsSpan(0, read));
                length += (uint)read;
            }
            crc32 = crc.GetCurrentHashAsUInt32();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public struct Options()
    {
        public bool IgnoreDiskNumber = true;
        public bool ReCalculateEntryCount = true;
        public bool ReCalculateCRC32 = true;
        public bool ReCalculateUncompressedSize = true;
    }
}