using ResourcePackRepairer.PNG;
using ResourcePackRepairer.ZIP;

namespace ResourcePackRepairer;

internal static partial class Program
{
    internal static void Main()
    {
        Console.WriteLine("Mode[0:zip/1:png]");
        ReadOnlySpan<char> mode = Console.ReadLine().AsSpan().Trim();

        Console.WriteLine("Input");
        string nIn = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        using FileStream fIn = File.Open(nIn, FileMode.Open, FileAccess.Read, FileShare.Read);

        Console.WriteLine("Output");
        string nOut = Console.ReadLine().AsSpan().Trim().Trim('"').ToString();
        using FileStream fOut = File.Open(nOut, FileMode.Create, FileAccess.Write, FileShare.Read);

        switch (mode)
        {
            case "0":
                ZIPRepairer.Repair(fIn, fOut);
                break;
            case "1":
                PNGRepairer.Repair(fIn, fOut);
                break;
            default:
                Console.WriteLine("Unknown mode!");
                break;
        }
    }
}