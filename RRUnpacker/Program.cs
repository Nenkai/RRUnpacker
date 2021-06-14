using System;
using System.IO;

using RRUnpacker.TOC;

using CommandLine;
using CommandLine.Text;

namespace RRUnpacker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RRUnpacker for Ridge Racer PSP(V2)/6/7/PSVita & R:Racing Evolution - by Nenkai#9075");
            Console.WriteLine();

            Parser.Default.ParseArguments<RR7Verbs, RR6Verbs, RRNVerbs, RRPSPVerbs, RREVerbs>(args)
                .WithParsed<RR7Verbs>(RR7Action)
                .WithParsed<RR6Verbs>(RR6Action)
                .WithParsed<RRPSPVerbs>(RRPSPAction)
                .WithParsed<RRNVerbs>(RRNAction)
                .WithParsed<RREVerbs>(RREAction);

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

            var toc = new RR7TableOfContents(options.GameCode, options.ElfPath);
            toc.Read();

            var unpacker = new RRUnpacker<RR7TableOfContents>(options.InputPath, options.OutputPath);
            unpacker.SetToc(toc);
            unpacker.ExtractContainers();
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

            var toc = new RR6TableOfContents(options.XexPath);
            toc.Read();

            var unpacker = new RRUnpacker<RR6TableOfContents>(options.InputPath, options.OutputPath);
            unpacker.SetToc(toc);
            unpacker.ExtractContainers();
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

            var toc = new RRPTableOfContents(options.ElfPath);
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

            var toc = new RRETableOfContents(options.ElfPath);
            toc.Read();

            var unpacker = new RRUnpacker<RRETableOfContents>(options.InputPath, options.OutputPath);
            unpacker.SetToc(toc);
            unpacker.ExtractContainers();
        }
    }



    [Verb("rr7", HelpText = "Unpacks .DAT files for Ridge Racer 7 (PS3).")]
    public class RR7Verbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RR7.DAT.")]
        public string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decrypted. Example: main.elf.")]
        public string ElfPath { get; set; }

        [Option('g', "gamecode", Required = true, HelpText = "Game Code of the game. Example: NPEB00513")]
        public string GameCode { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }

    [Verb("rr6", HelpText = "Unpacks .DAT files for Ridge Racer 6 (X360).")]
    public class RR6Verbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRM.DAT/RRM2.DAT/RRM3.DAT.")]
        public string InputPath { get; set; }

        [Option('x', "xex-path", Required = true, HelpText = "Input .xex file. MUST be decrypted through XeXTool.")]
        public string XexPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }

    [Verb("rrn", HelpText = "Unpacks .DAT files for Ridge Racer (PS Vita).")]
    public class RRNVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRN.DAT.")]
        public string InputPath { get; set; }

        [Option("info", Required = true, HelpText = "Input .info linked to the .dat file which should be next to it.")]
        public string InfoPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }

    [Verb("rrpsp", HelpText = "Unpacks .DAT files for Ridge Racer Version 2 (PSP).")]
    public class RRPSPVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRP.DAT.")]
        public string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file that should already be decrypted. Example: BOOT.elf.")]
        public string ElfPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }

    [Verb("rre", HelpText = "Unpacks .DAT files for R:Racing Evolution.")]
    public class RREVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file, should be RGC.DAT.")]
        public string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .elf file of the game. Example: SLES_523.09.")]
        public string ElfPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }
}
