# ResourcePackRepairer

A tool for repairing Minecraft resource pack files. It primarily focuses on files that are missing or contain false metadata, as output by obfuscation/protection tools.

Currently, it can repair the following types of file corruption:

- ZIP files with incorrect Local File Headers
- ZIP files with incorrect CRC-32 checksums
- ZIP files with incorrect uncompressed lengths
- ZIP files with incorrect disk numbers
- ZIP files with incorrect entry count
- PNG files with incorrect CRC-32 checksums
- PNG files with incorrect Adler-32 checksums in IDAT chunk

TODOs:

- ZIP-64 files
- Parsing of command-line arguments

> [!NOTE]
>
> This tool assumes checksums and many other metadata are unreliable and will attempt to recalculate the checksums from the data.
>
> Furthermore, if your file is truly corrupted in its data portion, this tool can only restore it to a well-formed file at best, not recover the corrupted data.

> [!IMPORTANT]
>
> These corrupted, malformed or non-standard files typically originate from resource packs processed by obfuscation/protection tools. Do not use the extracted files for purposes other than study and research without the copyright holder's permission.
