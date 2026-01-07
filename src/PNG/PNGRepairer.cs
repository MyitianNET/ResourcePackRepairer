using System.Buffers;
using System.Buffers.Binary;

namespace ResourcePackRepairer.PNG;

public static class PNGRepairer
{
    public static ReadOnlySpan<byte> Signature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public const uint IDAT = ('I' << 24) | ('D' << 16) | ('A' << 8) | 'T';
    public const uint IEND = ('I' << 24) | ('E' << 16) | ('N' << 8) | 'D';
    public static void Repair(Stream source, Stream destination)
    {
        Repair(source, destination, new());
    }
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
            if (options.ReCalculateIDATAdler32 && !doneIDAT && chunk.Name == IDAT)
            {
                int available = chunk.Length;
                int offset = 0;
                if (adler32remaining == 0)
                {
                    if (inflater == null)
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
                    Span<byte> target = chunk.UnderlyingArray.AsSpan(offset);
                    if (available >= adler32remaining)
                    {
                        adler32value[^adler32remaining..].CopyTo(target);
                        doneIDAT = true;
                        adler32remaining = 0;
                    }
                    else
                    {
                        // in rare cases, an Adler-32 checksum might be splited into 2 IDAT chunks.
                        adler32value.Slice(4 - adler32remaining, available).CopyTo(target);
                        adler32remaining -= available;
                    }
                }
            }
            if (options.ReCalculateCRC32)
                chunk.ReCalculateCRC32();
            chunk.WriteToStream(destination);
            if (chunk.Name == IEND)
                break;
        }
    }

    internal static void InflaterToAdler32(IDisposable inflater, Adler32 adler32)
    {
        const int BufferSize = 65536;

        byte[] array = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (true)
            {
                int read = InflaterAccessor.Inflate(inflater, array);
                if (read <= 0)
                    break;
                adler32.Append(array.AsSpan(0, read));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(array);
        }
    }

    public struct Options()
    {
        public bool ReCalculateCRC32 = true;
        public bool ReCalculateIDATAdler32 = true;
    }
}