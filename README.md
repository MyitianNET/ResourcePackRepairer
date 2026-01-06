# ResourcePackRepairer

A tool for repairing Minecraft resource pack files.

It can currently repair the following file corruption types:

- ZIP files with incorrect Local File Headers
- ZIP files with incorrect CRC32 checksums
- ZIP files with incorrect uncompressed lengths

File corruption types not yet supported but planned:

- PNG files with incorrect CRC32 checksums
- PNG files with incorrect Adler-32 checksums in IDAT chunk

> [!NOTE]
>
> This tool assumes checksums are unreliable and will attempt to recalculate the checksums from the data.
>
> Furthermore, if your file is truly corrupted in its data portion, this tool can only restore it to a well-formed file at best, not recover the corrupted data.

> [!IMPORTANT]
>
> These corrupted, malformed or non-standard files typically originate from resource packs processed by resource pack protection tools. Do not use the extracted files for purposes other than study and research without the copyright holder's permission.
