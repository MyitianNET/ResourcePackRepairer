# ResourcePackRepairer

A tool for repairing Minecraft resource pack files. It primarily focuses on files that are missing or contain false metadata, as output by obfuscation/protection tools.

Currently, it can repair the following types of file corruption:

- ZIP file:
  - incorrect Local File Header
  - incorrect CRC-32 checksum
  - incorrect compressed size
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
> Furthermore, if your file is truly corrupted in its data portion, this tool can only restore it to a well-formed file at best, not recover the corrupted data.

> [!NOTE]
>
> The ZIP-64 implementation is not fully tested. When you find a bug, please attach the file that triggered the bug to the issue.
