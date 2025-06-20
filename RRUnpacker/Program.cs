using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using RRUnpacker.TOC;
using RR.Files.Database;

using CommandLine;
using CommandLine.Text;
using RRUnpacker.Headers;
using RRUnpacker.Decompressors;

namespace RRUnpacker;

class Program
{
    public const string Version = "2.1.0";

    static void Main(string[] args)
    {
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine($"- RRUnpacker {Version} for Ridge Racer PSP/6/7/PSVita, R:Racing, Go Vacation by Nenkai");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("- https://github.com/Nenkai");
        Console.WriteLine("- https://twitter.com/Nenkaai");
        Console.WriteLine("-----------------------------------------");
        Console.WriteLine("");

        Parser.Default.ParseArguments<RR7Verbs, RR7PackVerbs, RR6Verbs, RRNVerbs, RRPSPVerbs, RREVerbs, GoVacationVerbs, WeSkiAndSnowboardVerbs,
            ExportDbVerbs, ImportDbVerbs,
            DecompressVerbs>(args)
            .WithParsed<RR7Verbs>(RR7Action)
            .WithParsed<RR7PackVerbs>(RR7PackAction)
            .WithParsed<RR6Verbs>(RR6Action)
            .WithParsed<RRPSPVerbs>(RRPSPAction)
            .WithParsed<RRNVerbs>(RRNAction)
            .WithParsed<RREVerbs>(RREAction)
            .WithParsed<GoVacationVerbs>(GoVacationAction)
            .WithParsed<GoVacationSwitchVerbs>(GoVacationSwitchAction)
            .WithParsed<WeSkiAndSnowboardVerbs>(WeSkiAndSnowboardVerbsAction)

            .WithParsed<ExportDbVerbs>(ExportDb)

            .WithParsed<DecompressVerbs>(DecompressAction);
            //.WithParsed<ImportDbVerbs>(ImportDb);
    }

    public static void RR7Action(RR7Verbs options)
    {
        if (!File.Exists(options.ElfPath))
        {
            Console.WriteLine($"Provided ELF file '{options.ElfPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new RR7TableOfContents(options.GameCode, options.ElfPath);
        toc.Read();

        var unpacker = new RRUnpacker<RR7TableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RR7PackAction(RR7PackVerbs options)
    {
        if (!File.Exists(options.ElfPath))
        {
            Console.WriteLine($"Provided ELF file '{options.ElfPath}' does not exist.");
            return;
        }

        if (!Directory.Exists(options.ModFolder))
        {
            Console.WriteLine($"Mod folder '{options.ModFolder}' does not exist.");
            return;
        }


        var toc = new RR7TableOfContents(options.GameCode, options.ElfPath);
        toc.Read();

        using RR7Patcher patcher = new RR7Patcher(toc, options.ElfPath, options.InputPath);
        patcher.Patch(options.ModFolder);
    }

    public static void RR6Action(RR6Verbs options)
    {
        if (!File.Exists(options.XexPath))
        {
            Console.WriteLine($"Provided XEX file '{options.XexPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        if (!CheckXEX(options.XexPath))
            return;

        var toc = new RR6TableOfContents(options.XexPath);
        toc.Read();

        var unpacker = new RRUnpacker<RR6TableOfContents>(options.InputPath, options.OutputPath);
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

    public static void RRNAction(RRNVerbs options)
    {
        if (!File.Exists(options.InfoPath))
        {
            Console.WriteLine($"Provided Info file '{options.InfoPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new RRNTableOfContents(options.InfoPath);
        toc.Read();

        var unpacker = new RRUnpacker<RRNTableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RRPSPAction(RRPSPVerbs options)
    {
        if (!File.Exists(options.ElfPath))
        {
            Console.WriteLine($"Provided Elf file '{options.ElfPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new RRPTableOfContents(options.GameCode, options.ElfPath);
        toc.Read();

        var unpacker = new RRUnpacker<RRPTableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void RREAction(RREVerbs options)
    {
        if (!File.Exists(options.ElfPath))
        {
            Console.WriteLine($"Provided ELF file '{options.ElfPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new RRETableOfContents(options.ElfPath);
        toc.Read();

        var unpacker = new RRUnpacker<RRETableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }


    public static void GoVacationAction(GoVacationVerbs options)
    {
        if (!File.Exists(options.BinPath))
        {
            Console.WriteLine($"Provided BIN000 file '{options.BinPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new GoVacationWiiTableOfContents(options.InputPath, options.BinPath);
        toc.Read();

        var unpacker = new RRUnpacker<GoVacationWiiTableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void GoVacationSwitchAction(GoVacationSwitchVerbs options)
    {
        if (!File.Exists(options.ElfPath))
        {
            Console.WriteLine($"Provided ELF file '{options.ElfPath}' does not exist.");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new GoVacationNXTableOfContents(options.InputPath, options.ElfPath);
        toc.Read();

        var unpacker = new RRUnpacker<GoVacationNXTableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void WeSkiAndSnowboardVerbsAction(WeSkiAndSnowboardVerbs options)
    {
        if (!File.Exists(options.DolPath))
        {
            Console.WriteLine($"Provided dol file '{options.DolPath}' does not exist (it should point to main.dol).");
            return;
        }

        if (!File.Exists(options.InputPath))
        {
            Console.WriteLine($"Provided .DAT file '{options.InputPath}' does not exist.");
            return;
        }

        if (!string.IsNullOrEmpty(options.OutputPath) && !Directory.Exists(options.OutputPath))
        {
            Console.WriteLine($"Provided output path '{options.OutputPath}' does not exist.");
            return;
        }

        if (string.IsNullOrEmpty(options.OutputPath))
        {
            var dirInfo = new FileInfo(options.InputPath).DirectoryName;
            if (dirInfo is not null)
                options.OutputPath = Path.Combine(dirInfo, $"{Path.GetFileNameWithoutExtension(options.InputPath)}_extracted");
            else
                options.OutputPath = new FileInfo(options.InputPath).DirectoryName ?? "extracted";
        }

        var toc = new WeSkiAndSnowboardTableOfContents(options.InputPath, options.DolPath);
        toc.Read();

        var unpacker = new RRUnpacker<WeSkiAndSnowboardTableOfContents>(options.InputPath, options.OutputPath);
        unpacker.SetToc(toc);
        unpacker.ExtractContainers();
    }

    public static void DecompressAction(DecompressVerbs options)
    {
        if (Directory.Exists(options.InputPath))
        {
            foreach (var file in Directory.GetFiles(options.InputPath, "*", SearchOption.AllDirectories))
            {
                DecompressFile(file);
            }
        }
        else if (File.Exists(options.InputPath))
        {
            DecompressFile(options.InputPath);
        }
        else
        {
            Console.WriteLine($"ERROR: File or folder {options.InputPath} does not exist.");
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
    public static void ExportDb(ExportDbVerbs options)
    {
        RRDatabaseManager db;
        if (Directory.Exists(options.InputPath))
        {
            db = RRDatabaseManager.FromDirectory(options.InputPath);
            db.ExportAllToCSV(options.OutputPath ?? Path.GetDirectoryName(Path.GetFullPath(options.InputPath)));
        }
        else if (File.Exists(options.InputPath))
        {
            db = new RRDatabaseManager();
            var table = new Table(options.InputPath);
            table.Read();

            db.Tables.Add(table);
        }
        else
        {
            Console.WriteLine("ERROR: File does not exist.");
            return;
        }


        if (options.ExportAs == "CSV")
        {
            db.ExportAllToCSV(options.OutputPath ?? Path.GetDirectoryName(Path.GetFullPath(options.InputPath)));
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

    [Verb("rr7", HelpText = "Unpacks .DAT files for Ridge Racer 7 (PS3).")]
    public class RR7Verbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RR7.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decrypted. Example: main.elf.")]
        public required string ElfPath { get; set; }

        [Option('g', "gamecode", Required = true, HelpText = "Game Code of the game. Example: NPEB00513")]
        public required string GameCode { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public required string OutputPath { get; set; }
    }

    [Verb("rr7pack")]
    public class RR7PackVerbs
    {
        [Option('m', "mod-folder", Required = true, HelpText = "Input Mod folder")]
        public required string ModFolder { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decrypted. Example: main.elf.")]
        public required string ElfPath { get; set; }

        [Option('g', "gamecode", Required = true, HelpText = "Game Code of the game. Example: NPEB00513")]
        public required string GameCode { get; set; }

        [Option('i', Required = true, HelpText = "Input .DAT file like RR7.DAT.")]
        public required string InputPath { get; set; }
    }

    [Verb("rr6", HelpText = "Unpacks .DAT files for Ridge Racer 6 (X360).")]
    public class RR6Verbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRM.DAT/RRM2.DAT/RRM3.DAT.")]
        public required string InputPath { get; set; }

        [Option('x', "xex-path", Required = true, HelpText = "Input .xex file. MUST be decrypted through XeXTool.")]
        public required string XexPath { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("rrn", HelpText = "Unpacks .DAT files for Ridge Racer (PS Vita).")]
    public class RRNVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRN.DAT.")]
        public required string InputPath { get; set; }

        [Option("info", Required = true, HelpText = "Input .info linked to the .dat file which should be next to it.")]
        public required string InfoPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("rrpsp", HelpText = "Unpacks .DAT files for Ridge Racer Version 2 (PSP).")]
    public class RRPSPVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRP.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decrypted. Example: BOOT.elf.")]
        public required string ElfPath { get; set; }

        [Option('g', "gamecode", Required = true, HelpText = "Game Code of the game. Example: UCES00422")]
        public required string GameCode { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("rre", HelpText = "Unpacks .DAT files for R:Racing Evolution.")]
    public class RREVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file, should be RGC.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file of the game. Example: SLES_523.09.")]
        public required string ElfPath { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("gv-wii", HelpText = "Unpacks .DAT files for Go Vacation (Wii).")]
    public class GoVacationVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like DISC.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "bin-path", Required = true, HelpText = "Input BIN000.DAT.")]
        public required string BinPath { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("gv-switch", HelpText = "Unpacks .DAT files for Go Vacation (Switch).")]
    public class GoVacationSwitchVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like DISC.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decompressed using nx2elf2nso. Example: main.elf")]
        public required string ElfPath { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("wgas", HelpText = "Unpacks .DAT files for We Ski & Snowboard.")]
    public class WeSkiAndSnowboardVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like SKI.DAT.")]
        public required string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .dol file. Example: main.dol")]
        public required string DolPath { get; set; }

        [Option('o', "output", HelpText = "Output directory for the extracted files.")]
        public string? OutputPath { get; set; }
    }

    [Verb("export-db", HelpText = "Exports a RR database file to SQLite or CSV.")]
    public class ExportDbVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file or folder.")]
        public required string InputPath { get; set; }

        [Option('o', "output", HelpText = "Output dir (if CSV) or output file (if SQLite).")]
        public string? OutputPath { get; set; }

        [Option("export-as", Default = "CSV", HelpText = "How to export. Defaults to SQLite. Options: CSV")]
        public string? ExportAs { get; set; }
    }

    [Verb("import-db", HelpText = "Imports a database file as SQLite and exports it to a RR database file.")]
    public class ImportDbVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file or folder.")]
        public required string InputPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file.")]
        public string? OutputPath { get; set; }

        [Option("little-endian", HelpText = "Whether to import the database as little-endian (for PS Vita Ridge Racer). Defaults to false (BE).")]
        public bool LittleEndian { get; set; }
    }

    [Verb("decompress", HelpText = "Decompresses a file (starting with hex bytes 5A 3F 2E 00 (0x5A3F2E00) found in Go Vacation and possibly other games.\n" +
        "Examples: STATIC.DAT, files in BIN000.DAT")]
    public class DecompressVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input file or folder.")]
        public required string InputPath { get; set; }
    }
}
