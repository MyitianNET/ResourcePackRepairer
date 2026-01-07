using System.Runtime.InteropServices;

namespace ResourcePackRepairer.ZIP;

public interface IDataStruct
{
    void ReverseEndianness();

    public static bool TryReadFromStream<T>(Stream stream, out T header) where T : struct, IDataStruct
    {
        header = default;
        Span<byte> buffer = MemoryMarshal.AsBytes(new Span<T>(ref header));
        if (!stream.TryReadExactly(buffer))
            return false;
        if (!BitConverter.IsLittleEndian)
            header.ReverseEndianness();
        return true;
    }
    public static T ReadExactlyFromStream<T>(Stream stream) where T : struct, IDataStruct
    {
        T header = default;
        Span<byte> buffer = MemoryMarshal.AsBytes(new Span<T>(ref header));
        stream.ReadExactly(buffer);
        if (!BitConverter.IsLittleEndian)
            header.ReverseEndianness();
        return header;
    }
    public static void WriteToStream<T>(Stream stream, T header) where T : struct, IDataStruct
    {
        if (!BitConverter.IsLittleEndian)
            header.ReverseEndianness();
        stream.Write(MemoryMarshal.AsBytes(new Span<T>(ref header)));
    }
}