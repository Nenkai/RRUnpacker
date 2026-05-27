// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using RR.Files.Database;

using RRUnpacker.Decompressors;
using RRUnpacker.Headers;
using RRUnpacker.TOC;

namespace RRUnpacker;

class Program
{
    public const string Version = "2.3.0";

    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine($"- RRUnpacker {Version} by Nenkai -");
        Console.WriteLine("- Ridge Racer PSP/6/7/PSVita");
        Console.WriteLine("- R:Racing Evolution (PS2)");
        Console.WriteLine("- Go Vacation (Wii/Nintendo Switch)");
        Console.WriteLine("- We Ski (Wii)");
        Console.WriteLine("- We Ski & Snowboard (Wii)");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("- https://github.com/Nenkai");
        Console.WriteLine("- https://twitter.com/Nenkaai");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("");

        ///////////////////////////////////////
        ///            EXTRACTORS            //
        ///////////////////////////////////////
        var rr7Command = new Command("rr7", "Unpacks .DAT files for Ridge Racer 7 (PS3).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like RR7.DAT."},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description = "Input .elf file that should already be decrypted. Example: main.elf."},
            new Option<string>("--gamecode", aliases: ["-g"]) { Required = true, Description = "Game Code of the game. Example: NPEB00513"},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        rr7Command.SetAction(RR7Action);

        var rr7PackCommand = new Command("rr7pack", "Appends modded files to a .DAT file (and updates the elf executable).")
        {
            new Option<string>("--mod-folder", aliases: ["-m"]) { Required = true, Description = "Input Mod folder"},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description = "Input .elf file that should already be decrypted. Example: main.elf."},
            new Option<string>("--gamecode", aliases: ["-g"]) { Required = true, Description = "Game Code of the game. Example: NPEB00513"},
            new Option<string?>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like RR7.DAT."}
        };
        rr7PackCommand.SetAction(RR7PackAction);

        var rr6Command = new Command("rr6", "Unpacks .DAT files for Ridge Racer 6 (X360).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like RRM.DAT/RRM2.DAT/RRM3.DAT."},
            new Option<FileInfo>("--xex-path", aliases: ["-x"]) { Required = true, Description = "Input .xex file. MUST be decrypted through XeXTool."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        rr6Command.SetAction(RR6Action);

        var rrnCommand = new Command("rrn", "Unpacks .DAT files for Ridge Racer (PS Vita).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like RRN.DAT."},
            new Option<FileInfo>("--info") { Required = true, Description = "Input .info linked to the .dat file which should be next to it."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        rrnCommand.SetAction(RRNAction);

        var rrpspCommand = new Command("rrpsp", "Unpacks .DAT files for Ridge Racer Version 2 (PSP).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like RRP.DAT."},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description = "Input .elf file that should already be decrypted. Example: BOOT.elf."},
            new Option<string>("--gamecode", aliases: ["-g"]) { Required = true, Description = "Game Code of the game. Example: UCES00422"},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        rrpspCommand.SetAction(RRPSPAction);

        var rreCommand = new Command("rre", "Unpacks .DAT files for R:Racing Evolution.")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file, should be RGC.DAT."},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description =  "Input .elf file of the game. Example: SLES_523.09."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        rreCommand.SetAction(RREAction);

        var gvWiiCommand = new Command("gv-wii", "Unpacks .DAT files for Go Vacation (Wii).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like DISC.DAT."},
            new Option<FileInfo>("--bin-path", aliases: ["-e"]) { Required = true, Description =  "Input BIN000.DAT."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        gvWiiCommand.SetAction(GoVacationAction);

        var gvSwitchCommand = new Command("gv-switch", "Unpacks .DAT files for Go Vacation (Nintendo Switch).")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like DISC.DAT."},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description =  "Input .elf file that should already be decompressed using nx2elf2nso. Example: main.elf"},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        gvSwitchCommand.SetAction(GoVacationSwitchAction);

        var weskiCommand = new Command("weski", "Unpacks .DAT files for We Ski / We Ski & Snowboard.")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input .DAT file like SKI.DAT."},
            new Option<FileInfo>("--elf-path", aliases: ["-e"]) { Required = true, Description = "Input .dol file. Example: main.dol"},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output directory for the extracted files."}
        };
        weskiCommand.SetAction(WeSkiAndSnowboardVerbsAction);

        ///////////////////////////////////////
        ///             DATABASE             //
        ///////////////////////////////////////
        var exportDbCommand = new Command("export-db", "Exports a RR database file to SQLite or CSV.")
        {
            new Option<string>("--input", aliases: ["-i"]) { Required = true, Description = "Input file or folder."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output dir (if CSV) or output file (if SQLite)."},
            new Option<string>("--export-as", aliases: ["-e"]) { Required = true, Description = "How to export. Defaults to CSV. Options: CSV",
                DefaultValueFactory = (res) => { return "CSV"; } }
        };
        exportDbCommand.SetAction(ExportDb);

        /*
        var importDbCommand = new Command("import-db", "Imports a database file as SQLite and exports it to a RR database file.")
        {
            new Option<FileInfo>("--input", aliases: ["-i"]) { Required = true, Description = "Input file or folder."},
            new Option<string>("--output", aliases: ["-o"]) { Description = "Output file."},
            new Option<bool>("--little-endian", aliases: ["-le"]) { Description = "Whether to import the database as little-endian (for PS Vita Ridge Racer). Defaults to false (BE)." },
        };
        importDbCommand.SetAction(ImportDb);
        */

        ///////////////////////////////////////
        ///               UTILS              //
        ///////////////////////////////////////
        var decompressCommand = new Command("decompress", "Decompresses a file (starting with hex bytes 5A 3F 2E 00 (0x5A3F2E00) found in Go Vacation and possibly other games.\n" +
            "Examples: STATIC.DAT, files in BIN000.DAT")
        {
            new Option<string>("--input", aliases: ["-i"]) { Required = true, Description = "Input file or folder."},
        };
        decompressCommand.SetAction(DecompressAction);

        var rootCommand = new RootCommand("RRUnpacker")
        {
            rr7Command,
            rr7PackCommand,
            rr6Command,
            rrnCommand,
            rreCommand,
            gvWiiCommand,
            gvSwitchCommand,
            weskiCommand,

            exportDbCommand,
            //importDbCommand,

            decompressCommand
        };

        return await rootCommand.Parse(args).InvokeAsync();
    }

    public static void RR7Action(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        string gameCode = parseResult.GetRequiredValue<string>("--gamecode");
        FileInfo elfPath = parseResult.GetRequiredValue<FileInfo>("--elf-path");
        var toc = new RR7TableOfContents(gameCode, elfPath.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<RR7TableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RR7PackAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        string modFolder = parseResult.GetRequiredValue<string>("--mod-folder");
        FileInfo elfPath = parseResult.GetRequiredValue<FileInfo>("--elf-path");
        string gameCode = parseResult.GetRequiredValue<string>("--gamecode");

        var toc = new RR7TableOfContents(gameCode, elfPath.FullName);
        toc.Read();

        using RR7Patcher patcher = new RR7Patcher(toc, elfPath.FullName, inputFile.FullName);
        patcher.Patch(modFolder);
    }

    public static void RR6Action(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo xexPath = parseResult.GetRequiredValue<FileInfo>("--xex-path");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        if (!CheckXEX(xexPath.FullName))
            return;

        var toc = new RR6TableOfContents(xexPath.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<RR6TableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    private static bool CheckXEX(string fileName)
    {
        var header = XEX2Header.Read(fileName);
        if (header is null)
        {
            Console.WriteLine($"Could not read XEX header, xex file provided does not seem to be a Xbox 360 Executable (XEX).");
            return false;
        }

        XEX2OptHeader fileDataDescriptor = header.GetImageHeaderInfoByKey(XEX2ImageKeyType.FileDataDescriptor);
        if (fileDataDescriptor is null || fileDataDescriptor.Value is not XEX2FileDataDescriptor fd)
        {
            Console.WriteLine($"Possibly corrupted XEX file, xex provided does not have a file data descriptor.");
            return false;
        }

        if (fd.EncryptionType == XEX2EncryptionType.Encrypted)
        {
            Console.WriteLine($"XEX file is encrypted. Decrypt it with XeXTool first.");
            return false;
        }

        if (fd.CompressionType == XEX2CompressionType.Compressed)
        {
            Console.WriteLine($"XEX file is compressed. Decompress it with XeXTool first.");
            return false;
        }

        return true;
    }

    public static void RRNAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo infoFile = parseResult.GetRequiredValue<FileInfo>("--info");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new RRNTableOfContents(infoFile.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<RRNTableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RRPSPAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo elfPath = parseResult.GetRequiredValue<FileInfo>("--elf-path");
        string gameCode = parseResult.GetRequiredValue<string>("--gamecode");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new RRPTableOfContents(gameCode, elfPath.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<RRPTableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RREAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo elfPath = parseResult.GetRequiredValue<FileInfo>("--elf-path");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new RRETableOfContents(elfPath.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<RRETableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }


    public static void GoVacationAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo binFile = parseResult.GetRequiredValue<FileInfo>("--bin-path");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new GoVacationWiiTableOfContents(inputFile.FullName, binFile.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<GoVacationWiiTableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void GoVacationSwitchAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo elfPath = parseResult.GetRequiredValue<FileInfo>("--elf-path");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new GoVacationNXTableOfContents(inputFile.FullName, elfPath.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<GoVacationNXTableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void WeSkiAndSnowboardVerbsAction(ParseResult parseResult)
    {
        if (!CheckInputExists(parseResult, out FileInfo? inputFile))
            return;

        FileInfo dolFile = parseResult.GetRequiredValue<FileInfo>("--elf-path");

        if (!GetOutputPath(parseResult, inputFile, out string? outputPath))
            return;

        var toc = new WeSkiAndSnowboardTableOfContents(inputFile.FullName, dolFile.FullName);
        toc.Read();

        var unpacker = new RRUnpacker<WeSkiAndSnowboardTableOfContents>(inputFile.FullName, outputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void DecompressAction(ParseResult parseResult)
    {
        string inputPath = parseResult.GetRequiredValue<string>("--input");

        if (Directory.Exists(inputPath))
        {
            foreach (var file in Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories))
            {
                DecompressFile(file);
            }
        }
        else if (File.Exists(inputPath))
        {
            DecompressFile(inputPath);
        }
        else
        {
            Console.WriteLine($"ERROR: File or folder {inputPath} does not exist.");
            return;
        }
    }

    private static void DecompressFile(string fileName)
    {
        if (fileName.EndsWith(".dec"))
        {
            Console.WriteLine($"Skipping {Path.GetFileName(fileName)}, assuming already decompressed");
            return;
        }

        using var fs = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite);
        if (!RRLZDecompressor.DecompressWithHeader(fs, out byte[]? decompressedData))
        {
            Console.WriteLine($"Failed to decompress: {Path.GetFileName(fileName)}");
            return;
        }

        Console.WriteLine($"Decompressed -> {fileName}.dec");
        File.WriteAllBytes(fileName + ".dec", decompressedData);
        return;
    }

    //////////////////////////////////////////
    //// DB Stuff
    //////////////////////////////////////////
    public static void ExportDb(ParseResult parseResult)
    {
        string inputPath = parseResult.GetRequiredValue<string>("--input");
        string? outputPath = parseResult.GetValue<string>("--output");
        string exportAs = parseResult.GetRequiredValue<string>("--export-as");

        RRDatabaseManager db;
        if (Directory.Exists(inputPath))
        {
            db = RRDatabaseManager.FromDirectory(inputPath);
            db.ExportAllToCSV(outputPath ?? Path.GetDirectoryName(Path.GetFullPath(inputPath))!);
        }
        else if (File.Exists(inputPath))
        {
            db = new RRDatabaseManager();
            var table = new Table(inputPath);
            table.Read();

            db.Tables.Add(table);
        }
        else
        {
            Console.WriteLine("ERROR: File does not exist.");
            return;
        }


        if (exportAs == "CSV")
        {
            db.ExportAllToCSV(outputPath ?? Path.GetDirectoryName(Path.GetFullPath(inputPath))!);
            Console.WriteLine($"Done. Exported {db.Tables.Count} table(s) to CSV.");
        }
        /* Commented out for now. Check comment in SQLiteExporter.cs
        else if (options.ExportAs == "SQLite")
        {
            SQLiteExporter exporter = new SQLiteExporter(db);
            exporter.ExportToSQLite(options.OutputPath ?? options.InputPath + ".sqlite");
            Console.WriteLine($"Done. Exported {db.Tables.Count} table(s) to SQLite.");
        }
        */
        else
        {
            Console.WriteLine("Invalid export method");
        }
    }

    /* Commented out for now. Check comment in SQLiteExporter.cs, but we should be importing from CSV instead, which is not implemented yet
    public static void ImportDb(ImportDbVerbs options)
    {
        RRDatabaseManager db;
        if (File.Exists(options.InputPath))
        {
            db = RRDatabaseManager.FromSQLite(options.InputPath);
        }
        else
        {
            Console.WriteLine("File does not exist.");
            return;
        }

        Directory.CreateDirectory(options.OutputPath);
        db.Save(options.OutputPath, !options.LittleEndian);
        Console.WriteLine("Done.");
    }
    */

    private static bool CheckInputExists(ParseResult result, [NotNullWhen(true)] out FileInfo? inputFile)
    {
        inputFile = result.GetRequiredValue<FileInfo>("--input");
        if (!inputFile.Exists)
        {
            Console.WriteLine($"ERROR: File '{inputFile.FullName}' does not exist");
            return false;
        }

        return true;
    }

    private static bool GetOutputPath(ParseResult parseResult, FileInfo inputFile, [NotNullWhen(true)] out string? outputPath)
    {
        outputPath = parseResult.GetValue<string>("--output");
        if (!string.IsNullOrEmpty(outputPath) && !Directory.Exists(outputPath))
        {
            Console.WriteLine($"Provided output path '{outputPath}' does not exist.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            var dirInfo = inputFile.Directory?.FullName;
            if (dirInfo is not null)
                outputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(inputFile.FullName)}_extracted");
            else
                outputPath = new FileInfo(inputFile.FullName).DirectoryName ?? "extracted";
        }

        return true;
    }
}
