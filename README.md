# RRUnpacker
Ridge Racer PSP/6/7/PS Vita & Go Vacation - .DAT unpacker

Unpacks all files (including custom compressed ones) from .DAT files.
**Only the supported versions listed below are extractable.**

## Ridge Racer 6
Decrypted `default.xex` is required to extract RRM(2/3).DAT.

Support for:
* PAL Version

## Ridge Racer 7
Needs a decrypted main.self file to use alongside the RR7.DAT file.

Support for:
* `NPUB30457` - PSN US Version
* `NPEB00513` - PSN EU Version

## Ridge Racer PS Vita
Provide the info file linked to the .DAT file (usually next to the .DAT file).

## Ridge Racer PSP
Needs a decrypted boot.bin file to use alongside the DAT file.

Support for:
* `UCES00422` - Ridge Racer PSP Version 2
* `ULJS00080` - Ridge Racer PSP Version 2 (JP)
* `ULJS00001` - Ridge Racer PSP Version 1 (JP)

## R:Racing Evolution
Support for:
* `SLES_52309` - PS2 PAL Version

## Go Vacation (Switch)
Support for:
* `0100C1800A9B6000`

Make sure to convert `main` to `main.elf` first. Use [nx2elf2nso](https://archive.org/download/nx2elf2nso/nx2elf2nso.zip).

## Download
Download in [Releases](https://github.com/Nenkai/RRUnpacker/releases).
