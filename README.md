# RRUnpacker
Ridge Racer 6/7/PSP/VITA, R:Racing, Go Vacation, We Ski & Snowboard, We Ski - .DAT unpacker 

Unpacks all files (including custom compressed ones) from .DAT files.

> [!NOTE]  
>  ### **Only the supported versions listed below are extractable.**
>
> The table of contents for every RR game (except RR PS Vita) is directly embedded in the executables. It is a lengthy process to support manually.
> 
> In Go Vacation, it is embedded in RSO modules which are located in BIN000.DAT (U8 archive, then every module is compressed with the RRLZ compression wrapped in a `0x5A3F2E00` / `5A 3F 2E 00` header.
>
> In We Ski and We Ski & Snowboard, the table of contents is also directly embedded in the exacutables (main.dol) with hard-coded virtual memory addresses. Some analysis is required in order to add support.


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

### GameCube
Support for:
* `GRJP69` - R: RACING (Europe) (GRJP69)
* `GRJEAF` - R: RACING Evolution (USA)
* `GRJJAF` - R:RACING EVOLUTION (Japan)

### PS2

Support for:
* `SLES_52309` - PS2 PAL Version

## Go Vacation
Support for:
* `0100C1800A9B6000` Go Vacation (Switch) (Make sure to convert `main` to `main.elf` first. Use [nx2elf2nso](https://archive.org/download/nx2elf2nso/nx2elf2nso.zip))
* `SGVPAF` - Go Vacation (Wii) (Europe)
* `SGVEAF` - Go Vacation (Wii) (US)
* `SGVEAF` - Go Vacation (Wii) (US) (Prototype)

## We Ski & Snowboard
Support for:
* `RYKEAF` - We Ski & Snowboard (US)
* `RYKEAF` - We Ski & Snowboard (US) (Beta)
* `RYKEAF` - We Ski & Snowboard (Prototype)

## We Ski
Support for:
* `RSQEAF` - We Ski (US)
* `RSQEAF` - We Ski (US) (Beta)
* `RSQEAF` - We Ski (Prototype)

## Download
Download in [Releases](https://github.com/Nenkai/RRUnpacker/releases).
