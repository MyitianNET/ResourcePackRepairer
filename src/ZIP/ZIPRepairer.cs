using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;

namespace ResourcePackRepairer.ZIP;

public static class ZIPRepairer
{
    /// <param name="source">ZIP input stream, must be readable and seekable</param>
    /// <param name="destination">ZIP output stream, must be writeable, and the <see cref="Stream.Length"/> property must be readable</param>
    /// <exception cref="EndOfStreamException" />
    /// <exception cref="InvalidDataException" />
    /// <exception cref="NotSupportedException" />
    public static void Repair(Stream source, Stream destination)
    {
        Repair(source, destination, new());
    }
    /// <param name="source">ZIP input stream, must be readable and seekable</param>
    /// <param name="destination">ZIP output stream, must be writeable, and the <see cref="Stream.Length"/> property must be readable</param>
    /// <exception cref="EndOfStreamException" />
    /// <exception cref="InvalidDataException" />
    /// <exception cref="NotSupportedException" />
    public static void Repair(Stream source, Stream destination, in Options options)
    {
        const ushort Zip64VersionNeeded = 45;

        // Read EOCD from source
        EndOfCentralDirectory64 eocd64 = FindEOCD(source, options, out byte[] extras, out byte[] comment);
        if (options.IgnoreDiskNumber)
        {
            eocd64.DiskNumber = 0;
            eocd64.StartDiskNumber = 0;
        }
        else if (eocd64.DiskNumber != 0 || eocd64.StartDiskNumber != 0)
            throw new NotSupportedException("Spanned ZIP is not supported!");

        bool zip64required = true;
        if (eocd64.SizeOfRecord == 0)
        {
            // EOCD64 converted from EOCD will have a SizeOfRecord=0
            zip64required = false;
            eocd64.SizeOfExtras = extras.Length;
            eocd64.VersionMadeBy = Zip64VersionNeeded;
            eocd64.VersionNeeded = Zip64VersionNeeded;
        }

        source.Position = (long)eocd64.DirectoryOffset;
        long endCD = (long)(eocd64.DirectoryOffset + eocd64.DirectorySize);
        List<FullCentralDirectoryHeader> cdhList = [];
        ExtraFieldCollection lfhExtraFields = new();
        while (source.Position < endCD)
        {
            // Read CDH from source
            long pos = source.Position;
            if (!source.ReadForwardsUntilFind4ByteSeq(CentralDirectoryHeader.Signature))
                throw new InvalidDataException($"Cannot find CDH signature at {pos}!");
            FullCentralDirectoryHeader fcdh = FullCentralDirectoryHeader.ReadFromStream(source);
            fcdh.ExtraFields.ReadZip64ExtraField(
                fcdh.CDH.UncompressedSize, fcdh.CDH.CompressedSize, fcdh.CDH.LocalHeaderOffset, fcdh.CDH.StartDiskNumber,
                out ulong uncompressedSize, out ulong compressedSize, out ulong localHeaderOffset, out uint startDiskNumber);
            if (!options.IgnoreDiskNumber && startDiskNumber != 0)
                throw new NotSupportedException("Spanned ZIP is not supported!");
            if (localHeaderOffset > (ulong)source.Length)
                throw new InvalidDataException($"localHeaderOffset too large: {localHeaderOffset}!");

            // Read LFH from source
            pos = source.Position;
            source.Position = (long)localHeaderOffset;
            if (!source.ReadForwardsUntilFind4ByteSeq(LocalFileHeader.Signature))
                throw new InvalidDataException($"Cannot find LFH signature at {localHeaderOffset}!");
            LocalFileHeader lfh = IDataStruct.ReadExactlyFromStream<LocalFileHeader>(source);
            source.Seek(lfh.FileNameLength, SeekOrigin.Current);
            lfhExtraFields.ReadFromBytes(source.ReadBytes(lfh.ExtraFieldLength));
            long payloadStart = source.Position;

            // Try decompress to get correct CRC32 and size
            source.Position = payloadStart;
            AnalyzeBody(source, options, ref fcdh.CDH, ref uncompressedSize, ref compressedSize);

            // Save CDH to list
            localHeaderOffset = (ulong)destination.Length;
            if (fcdh.CDH.ApplyNewValue(fcdh.ExtraFields, uncompressedSize, compressedSize, localHeaderOffset, 0))
            {
                zip64required = true;
                fcdh.CDH.VersionMadeBy = Math.Max(fcdh.CDH.VersionMadeBy, Zip64VersionNeeded);
                fcdh.CDH.VersionNeeded = Math.Max(fcdh.CDH.VersionNeeded, Zip64VersionNeeded);
            }
            cdhList.Add(fcdh);

            // Write LFH to destination
            lfh = new(fcdh.CDH);
            if (lfh.ApplyNewValue(lfhExtraFields, uncompressedSize, compressedSize))
            {
                zip64required = true;
                lfh.VersionNeeded = Math.Max(lfh.VersionNeeded, Zip64VersionNeeded);
            }
            if (!lfhExtraFields.TryGetLengthInBytes(out lfh.ExtraFieldLength))
                throw new InvalidDataException($"Overlong ExtraField for {Encoding.UTF8.GetString(fcdh.FileName)}!");
            destination.Write(LocalFileHeader.Signature);
            IDataStruct.WriteToStream(destination, lfh);
            destination.Write(fcdh.FileName, 0, fcdh.FileName.Length);
            lfhExtraFields.WriteToStream(destination);

            // Copy compressed data from source to destination
            source.Position = payloadStart;
            source.LengthCopy(destination, compressedSize);
            source.Position = pos;
        }
        ulong cdhStartPos = (ulong)destination.Length;
        foreach (FullCentralDirectoryHeader fcdh in cdhList)
        {
            // Write CDH to destination
            fcdh.WriteToStream(destination);
        }

        // Write EOCD to destination
        eocd64.DirectoryOffset = cdhStartPos;
        eocd64.DirectorySize = (ulong)destination.Length - cdhStartPos;
        if (options.ReCalculateEntryCount)
            eocd64.TotalEntries = eocd64.EntriesOnThisDisk = (uint)cdhList.Count;
        EndOfCentralDirectory eocd = EndOfCentralDirectory.CreateFromEOCD64(eocd64, ref zip64required);
        eocd.CommentLength = (ushort)comment.Length;
        if (zip64required)
        {
            long eocd64offset = destination.Length;
            destination.Write(EndOfCentralDirectory64.Signature);
            IDataStruct.WriteToStream(destination, eocd64);
            destination.Write(extras);
            destination.Write(EndOfCentralDirectory64Locator.Signature);
            IDataStruct.WriteToStream(destination, new EndOfCentralDirectory64Locator()
            {
                DiskNumber = 0,
                RelativeOffset = (ulong)eocd64offset,
                TotalDisks = 1
            });
        }
        destination.Write(EndOfCentralDirectory.Signature);
        IDataStruct.WriteToStream(destination, eocd);
        destination.Write(comment, 0, comment.Length);
    }

    private static EndOfCentralDirectory64 FindEOCD(Stream stream, Options options, out byte[] extras, out byte[] comment)
    {
        if (!EndOfCentralDirectory.FindFromStream(stream, out EndOfCentralDirectory eocd, out EndOfCentralDirectory64Locator? locator))
            throw new InvalidDataException("Cannot find EOCD!");
        extras = [];
        comment = stream.ReadBytes(eocd.CommentLength);
        do
        {
            if (!locator.HasValue)
                break;
            if (!options.IgnoreDiskNumber && (locator.Value.DiskNumber != 0 || locator.Value.TotalDisks != 1))
                throw new NotSupportedException("Spanned ZIP is not supported!");
            ulong offset = locator.Value.RelativeOffset;
            if (offset > (ulong)stream.Length)
                break;
            stream.Position = (long)offset;
            if (!stream.StartsWith(EndOfCentralDirectory64.Signature)
                || !IDataStruct.TryReadFromStream(stream, out EndOfCentralDirectory64 eocd64)
                || eocd64.DirectoryOffset > long.MaxValue
                || eocd64.DirectorySize > long.MaxValue
                || eocd64.DirectoryOffset + eocd64.DirectorySize > (ulong)stream.Length
                || eocd64.SizeOfRecord > EndOfCentralDirectory64.MaxAllowedSize)
                break;
            if (options.ValidateEOCD64ByEOCD)
            {
                if (eocd.DirectorySize != uint.CreateSaturating(eocd64.DirectorySize)
                    || eocd.DirectoryOffset != uint.CreateSaturating(eocd64.DirectoryOffset))
                    break;
            }
            extras = stream.ReadBytes(eocd64.SizeOfExtras);
            return eocd64;
        } while (false);
        return new(eocd);
    }
    private static void AnalyzeBody(Stream source, Options options, ref CentralDirectoryHeader cdh, ref ulong uncompressedSize, ref ulong compressedSize)
    {
        if ((cdh.GeneralPurposeFlag & 1) != 0) // Encryption
            return;
        switch (cdh.CompressionMethod)
        {
            case 0:
                if (options.ReCalculateUncompressedSize)
                    uncompressedSize = compressedSize;
                if (options.ReCalculateCRC32)
                {
                    using LengthLimitedStream lls = new(source, compressedSize);
                    Crc32 crc = new();
                    crc.Append(lls);
                    cdh.CRC32 = crc.GetCurrentHashAsUInt32();
                }
                break;
            case 8 when options.ReCalculateCRC32 || options.ReCalculateCompressedSize || options.ReCalculateUncompressedSize:
                long pos = source.Position;
                try
                {
                    AnalyzeDeflateStream(source, out uint crc32, out ulong compressedSizeLocal, out ulong uncompressedSizeLocal);
                    if (options.ReCalculateCRC32)
                        cdh.CRC32 = crc32;
                    if (options.ReCalculateCompressedSize)
                        compressedSize = compressedSizeLocal;
                    if (options.ReCalculateUncompressedSize)
                        uncompressedSize = uncompressedSizeLocal;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"OFFS: {pos}");
                    Console.WriteLine($"US: {uncompressedSize}");
                    Console.WriteLine($"CS: {compressedSize}");
                }
                break;
        }
    }
    private static void AnalyzeDeflateStream(Stream stream, out uint crc32, out ulong compressedSize, out ulong uncompressedSize)
    {
        const int BufferSize = 8192;
        using IDisposable inflater = InflaterAccessor.CreateInflater();
        Crc32 crc = new();
        compressedSize = 0;
        uncompressedSize = 0;
        PooledArrayHandle<byte> compressedBuffer = new(BufferSize);
        PooledArrayHandle<byte> uncompressedBuffer = new(BufferSize);
        while (true)
        {
            int read = InflaterAccessor.Inflate(inflater, uncompressedBuffer.Array);
            if (read > 0)
            {
                uncompressedSize += (uint)read;
                crc.Append(uncompressedBuffer.Array.AsSpan(0, read));
            }
            else if (InflaterAccessor.Finished(inflater))
            {
                compressedSize -= InflaterAccessor.GetAvailableIn(inflater);
                crc32 = crc.GetCurrentHashAsUInt32();
                return;
            }
            else if (InflaterAccessor.GetAvailableIn(inflater) == 0)
            {
                read = stream.Read(compressedBuffer.Array, 0, compressedBuffer.Array.Length);
                compressedSize += (uint)read;
                InflaterAccessor.SetInput(inflater, compressedBuffer.Array.AsMemory(0, read));
            }
        }
    }
    private static bool ApplyNewValue(this ref LocalFileHeader self, ExtraFieldCollection extraFields, ulong uncompressedSize, ulong compressedSize)
    {
        const int MaxBufferSize = sizeof(ulong) * 2;

        int firstZ64EF = extraFields.ExtraFields.FindIndex(IsZip64ExtraField);
        extraFields.ExtraFields.RemoveAll(IsZip64ExtraField);
        Span<byte> buffer = stackalloc byte[MaxBufferSize];
        int bufferSize = 0;
        self.UncompressedSize = WriteU32(buffer, ref bufferSize, uncompressedSize);
        self.CompressedSize = WriteU32(buffer, ref bufferSize, compressedSize);
        if (bufferSize > 0)
        {
            extraFields.ExtraFields.Insert(Math.Max(0, firstZ64EF), new()
            {
                ID = 0x0001,
                Data = buffer[..bufferSize].ToArray()
            });
            return true;
        }
        return false;
    }
    private static bool ApplyNewValue(this ref CentralDirectoryHeader self, ExtraFieldCollection extraFields, ulong uncompressedSize, ulong compressedSize, ulong localHeaderOffset, uint startDiskNumber)
    {
        const int MaxBufferSize = sizeof(ulong) * 3 + sizeof(uint);

        int firstZ64EF = extraFields.ExtraFields.FindIndex(IsZip64ExtraField);
        extraFields.ExtraFields.RemoveAll(IsZip64ExtraField);
        Span<byte> buffer = stackalloc byte[MaxBufferSize];
        int bufferSize = 0;
        self.UncompressedSize = WriteU32(buffer, ref bufferSize, uncompressedSize);
        self.CompressedSize = WriteU32(buffer, ref bufferSize, compressedSize);
        self.LocalHeaderOffset = WriteU32(buffer, ref bufferSize, localHeaderOffset);
        self.StartDiskNumber = WriteU16(buffer, ref bufferSize, startDiskNumber);
        if (bufferSize > 0)
        {
            extraFields.ExtraFields.Insert(Math.Max(0, firstZ64EF), new()
            {
                ID = 0x0001,
                Data = buffer[..bufferSize].ToArray()
            });
            return true;
        }
        return false;
    }
    private static bool IsZip64ExtraField(ExtraField it) => it.ID == 0x0001;
    private static uint WriteU32(Span<byte> buffer, ref int bufferSize, ulong value)
    {
        bool overflowed = false;
        uint result = value.CreateSaturatingU32(ref overflowed);
        if (overflowed)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(buffer[bufferSize..], value);
            bufferSize += sizeof(uint);
        }
        return result;
    }
    private static ushort WriteU16(Span<byte> buffer, ref int bufferSize, uint value)
    {
        bool overflowed = false;
        ushort result = value.CreateSaturatingU16(ref overflowed);
        if (overflowed)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[bufferSize..], value);
            bufferSize += sizeof(uint);
        }
        return result;
    }

    public struct Options()
    {
        public bool IgnoreDiskNumber = true;
        public bool ReCalculateEntryCount = true;
        public bool ReCalculateCRC32 = true;
        public bool ReCalculateCompressedSize = true;
        public bool ReCalculateUncompressedSize = true;
        public bool ValidateEOCD64ByEOCD = true;
    }
}