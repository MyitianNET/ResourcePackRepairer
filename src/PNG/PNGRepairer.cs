using System.Buffers.Binary;

namespace ResourcePackRepairer.PNG;

public static class PNGRepairer
{
    public static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public const uint IDAT = ('I' << 24) | ('D' << 16) | ('A' << 8) | 'T';
    public const uint IEND = ('I' << 24) | ('E' << 16) | ('N' << 8) | 'D';

    /// <param name="source">PNG input stream, must be readable</param>
    /// <param name="destination">PNG output stream, must be writeable</param>
    /// <exception cref="InvalidDataException" />
    /// <exception cref="NotSupportedException" />
    public static void Repair(Stream source, Stream destination)
    {
        Repair(source, destination, new());
    }
    /// <param name="source">PNG input stream, must be readable</param>
    /// <param name="destination">PNG output stream, must be writeable</param>
    /// <exception cref="InvalidDataException" />
    /// <exception cref="NotSupportedException" />
    public static void Repair(Stream source, Stream destination, Options options)
    {
        // Read and write PNG Signature
        if (!source.StartsWith(Signature))
            throw new InvalidDataException("Source does not begin with PNG signature!");
        destination.Write(Signature);

        Adler32 adler32 = new();
        IDisposable? inflater = null;
        int adler32remaining = 0;
        Span<byte> adler32value = stackalloc byte[4];
        bool doneIDAT = false;

        // Chunk-by-chunk copy
        while (PNGChunk.TryReadFromStream(source, out PNGChunk chunk))
        {
            try
            {
                if (!options.ReCalculateIDATAdler32)
                    goto NEXT;
                if (doneIDAT)
                {
                    if (chunk.Name == IDAT)
                        goto SKIP; // no extra IDATs
                    else
                        goto NEXT;
                }
                if (chunk.Name != IDAT)
                {
                    if (adler32remaining != 0)
                    {
                        // the previous IDAT is not long enough to put the Adler-32 in
                        PNGChunk idat = PNGChunk.RentFromArrayPool(adler32remaining);
                        idat.Name = IDAT;
                        adler32value[^adler32remaining..].CopyTo(idat.Data);
                        doneIDAT = true;
                        adler32remaining = 0;
                        idat.ReCalculateCRC32();
                        idat.WriteToStream(destination);
                        idat.Dispose();
                    }
                    goto NEXT;
                }
                int available = chunk.Length;
                int offset = 0;
                if (adler32remaining == 0)
                {
                    if (inflater is null)
                    {
                        inflater = InflaterAccessor.CreateInflater();
                        available -= 2;
                        offset += 2; // skip ZLib header
                    }
                    InflaterAccessor.SetInput(inflater, chunk.UnderlyingArray.AsMemory(offset, available));
                    InflaterToAdler32(inflater, adler32);
                    if (InflaterAccessor.Finished(inflater))
                    {
                        int newAvailable = (int)InflaterAccessor.GetAvailableIn(inflater);
                        int diff = available - newAvailable;
                        offset += diff;
                        available = newAvailable;
                        inflater.Dispose();
                        inflater = null;
                        adler32remaining = 4;
                        BinaryPrimitives.WriteUInt32BigEndian(adler32value, adler32.GetCurrentHashAsUInt32());
                    }
                }
                if (adler32remaining != 0)
                {
                    Span<byte> target = chunk.Data[offset..];
                    if (available >= adler32remaining)
                    {
                        int remaining = available - adler32remaining;
                        if (remaining > 0)
                        {
                            // the chunk is too long!
                            chunk.Length -= remaining;
                        }
                        adler32value[^adler32remaining..].CopyTo(target);
                        adler32remaining = 0;
                        doneIDAT = true;
                    }
                    else
                    {
                        // in rare cases, an Adler-32 checksum might be splited into 2 IDAT chunks.
                        adler32value.Slice(4 - adler32remaining, available).CopyTo(target);
                        adler32remaining -= available;
                    }
                }
            NEXT:
                chunk.ReCalculateCRC32();
                chunk.WriteToStream(destination);
            SKIP:
                if (chunk.Name == IEND)
                    break;
            }
            finally
            {
                chunk.Dispose();
            }
        }
    }

    internal static void InflaterToAdler32(IDisposable inflater, Adler32 adler32)
    {
        const int BufferSize = 8192;
        using PooledArrayHandle<byte> array = new(BufferSize);
        while (true)
        {
            int read = InflaterAccessor.Inflate(inflater, array.Array);
            if (read <= 0)
                break;
            adler32.Append(array.Array.AsSpan(0, read));
        }
    }

    public struct Options()
    {
        public bool ReCalculateIDATAdler32 = true;
    }
}