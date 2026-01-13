using System.Reflection;
using System.Runtime.CompilerServices;

namespace ResourcePackRepairer;

// Warning!
// To simplify the codebase, reflection and UnsafeAccessorAttribute is used here to directly access
// System.IO.Compression.Inflater, which may introduce unreliability across different target .NET versions.
internal static class InflaterAccessor
{
    public const string InflaterType = "System.IO.Compression.Inflater, System.IO.Compression";
    public const string ZLibStreamHandleType = "System.IO.Compression.ZLibNative+ZLibStreamHandle, System.IO.Compression";

    // Future (.NET 11, not yet released but already in the repository)
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    [return: UnsafeAccessorType(InflaterType)]
    private static extern object CreateInflater(
        [UnsafeAccessorType(InflaterType)] object? self,
        int windowBits,
        long uncompressedSize = -1);

    // .NET 10
    [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
    [return: UnsafeAccessorType(InflaterType)]
    private static extern object CreateInflater(
        int windowBits,
        long uncompressedSize = -1);

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    public static extern int Inflate(
        [UnsafeAccessorType(InflaterType)] object self,
        Span<byte> destination);

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    public static extern void SetInput(
        [UnsafeAccessorType(InflaterType)] object self,
        ReadOnlyMemory<byte> inputBuffer);

    [UnsafeAccessor(UnsafeAccessorKind.Method)]
    public static extern bool Finished(
        [UnsafeAccessorType(InflaterType)] object self);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_AvailIn")]
    private static extern uint GetAvailIn(
        [UnsafeAccessorType(ZLibStreamHandleType)] object self);

    // Cannot use UnsafeAccessorTypeAttribute for field accessors' return value currently.
    // Fallback to reflection.
    private static readonly FieldInfo? Inflater_zlibStream = Type.GetType(InflaterType)
        ?.GetField("_zlibStream", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

    private static bool useAltCreateInflaterMethod = false;
    public static IDisposable CreateInflater()
    {
        const int Deflate_DefaultWindowBits = -15;
        if (!useAltCreateInflaterMethod)
        {
            try
            {
                return (IDisposable)CreateInflater(null, Deflate_DefaultWindowBits);
            }
            catch
            {
                useAltCreateInflaterMethod = true;
            }
        }
        return (IDisposable)CreateInflater(Deflate_DefaultWindowBits);
    }

    public static uint GetAvailableIn(IDisposable inflater)
    {
        object handle = (Inflater_zlibStream?.GetValue(inflater))
            ?? throw new NotSupportedException("Cannot get field _zlibStream");
        return GetAvailIn(handle);
    }
}