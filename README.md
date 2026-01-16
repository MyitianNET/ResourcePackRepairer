# ResourcePackRepairer

A tool for repairing Minecraft resource pack files. It primarily focuses on files that are missing or contain false metadata, as output by obfuscation/protection tools.

Currently, it can repair the following types of file corruption:

- ZIP file:
  - incorrect Local File Header
  - incorrect CRC-32 checksum
  - incorrect compressed size for deflate-compressed entry
  - incorrect uncompressed size
  - incorrect disk number
  - incorrect entry count
- PNG file:
  - incorrect CRC-32 checksum
  - incorrect Adler-32 checksum in IDAT chunk

> [!IMPORTANT]
>
> These corrupted, malformed or non-standard files typically originate from resource packs processed by obfuscation/protection tools. Do not use the extracted files for purposes other than study and research without the copyright holder's permission.

> [!NOTE]
>
> This tool assumes checksums and many other metadata are unreliable and will attempt to recalculate the checksums from the data.
>
> Furthermore, if your file is truly corrupted in its data portion, this tool will fail to decode or can only restore it to a well-formed file at best, not recover the corrupted data.

> [!NOTE]
>
> The ZIP-64 implementation is not fully tested. When you find a bug, please attach the file that triggered the bug to the issue.

## Usage

Command-line mode:

```
> ./ResourcePackRepairer.exe --help
Usage:
    ResourcePackRepairer [arguments...]

Arguments:
Name               | ParamCount | Alias  | Default | Info
--help             .          0 . -h, -? .         .
--mode             .          1 . -m     .         . Mode, accepted={zip|png}.
--input            .          1 . -i     .         . Input file path.
--output           .          1 . -o     .         . Output file path.
--in-memory-input  .          1 . -imi   . false   . Read entire input file into memory before processing.
--in-memory-output .          1 . -imo   . false   . Write output file after processing is complete.
> ./ResourcePackRepairer.exe -m <select mode here> -i <input file here> -o <output file here>
```

Interactive mode (when any of mode, input or output are not provided in command line arguments):

```
Mode[zip/png]: <select mode here>

Input file path: <input file here>

Output file path: <output file here>
```
