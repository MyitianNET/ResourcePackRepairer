using ResourcePackRepairer.PNG;
using ResourcePackRepairer.ZIP;
using SimpleArgs;

namespace ResourcePackRepairer;

static class Program
{
    public static void Main(string[] args)
    {
        ArgParser argx = new(args, ignoreCase: true,
            new("--help", 0, "-h", "-?"),
            new("--mode", 1, "-m")
            {
                Info = "Mode, accepted={zip|png}."
            },
            new("--input", 1, "-i")
            {
                Info = "Input file path."
            },
            new("--output", 1, "-o")
            {
                Info = "Output file path."
            },
            new("--in-memory-input", 1, "-imi")
            {
                Default = "false",
                Info = "Read entire input file into memory before processing."
            },
            new("--in-memory-output", 1, "-imo")
            {
                Default = "false",
                Info = "Write output file after processing is complete."
            });
        if (argx.Results.ContainsKey("--help"))
        {
            Console.Error.WriteLine("""
                Usage:
                    ResourcePackRepairer [arguments...]

                """);
            argx.WriteHelp(Console.Error);
            Console.Error.WriteLine();
            return;
        }
        if (!argx.TryGetString("--mode", out string? modeStr))
        {
            Console.Out.Write("Mode[zip/png]: ");
            modeStr = Console.In.ReadLine()?.Trim();
            Console.Out.WriteLine();
        }
        if (!Enum.TryParse(modeStr, true, out Mode mode) || !Enum.IsDefined(mode))
        {
            Console.Error.WriteLine("Invalid mode");
            return;
        }
        if (!argx.TryGetString("--input", out string? inputFile))
        {
            Console.Out.Write("Input file path: ");
            inputFile = Console.In.ReadLine().AsSpan().Trim().Trim('"').ToString();
            Console.Out.WriteLine();
        }
        if (!argx.TryGetString("--output", out string? outputFile))
        {
            Console.Out.Write("Output file path: ");
            outputFile = Console.In.ReadLine().AsSpan().Trim().Trim('"').ToString();
            Console.Out.WriteLine();
        }
        if (!argx.TryGetBoolean("--in-memory-input", out bool inMemIn))
        {
            Console.Error.WriteLine("Invalid boolean value for \"--in-memory-input\"");
            return;
        }
        if (!argx.TryGetBoolean("--in-memory-output", out bool inMemOut))
        {
            Console.Error.WriteLine("Invalid boolean value for \"--in-memory-output\"");
            return;
        }

        using Stream output = CreateOutput(outputFile, inMemOut);
        using (Stream input = CreateInput(inputFile, inMemIn))
        {
            switch (mode)
            {
                case Mode.ZIP:
                    ZIPRepairer.Repair(input, output);
                    break;
                case Mode.PNG:
                    PNGRepairer.Repair(input, output);
                    break;
            }
        }
        if (output is not FileStream)
        {
            using FileStream fs = File.Open(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            long pos = output.Position;
            output.Position = 0;
            fs.SetLength(output.Length);
            output.CopyTo(fs);
        }

        static Stream CreateInput(string file, bool inMemory)
        {
            FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (inMemory)
            {
                using (fs)
                {
                    SimpleUnmanagedMemoryStream ms = new(fs.Length);
                    fs.CopyTo(ms);
                    ms.Position = 0;
                    return ms;
                }
            }
            return fs;
        }
        static Stream CreateOutput(string file, bool inMemory)
        {
            return inMemory ?
                new SimpleUnmanagedMemoryStream() :
                File.Open(file, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
    }
    public static ushort CreateSaturatingU16(this uint number, ref bool overflowed)
    {
        if (number >= ushort.MaxValue)
        {
            overflowed = true;
            return ushort.MaxValue;
        }
        return (ushort)number;
    }
    public static ushort CreateSaturatingU16(this ulong number, ref bool overflowed)
    {
        if (number >= ushort.MaxValue)
        {
            overflowed = true;
            return ushort.MaxValue;
        }
        return (ushort)number;
    }
    public static uint CreateSaturatingU32(this ulong number, ref bool overflowed)
    {
        if (number >= uint.MaxValue)
        {
            overflowed = true;
            return uint.MaxValue;
        }
        return (uint)number;
    }

    public enum Mode
    {
        ZIP,
        PNG
    }
}