using System;
using System.IO;

using RRUnpacker.RR7;
using RRUnpacker.RRN;

using CommandLine;
using CommandLine.Text;

namespace RRUnpacker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RR7Unpacker (RR7.DAT) by Nenkai#9075");
            Console.WriteLine();

            Parser.Default.ParseArguments<RR7Verbs, RRNVerbs>(args)
                .WithParsed<RR7Verbs>(RR7Action)
                .WithParsed<RRNVerbs>(RRNAction);    
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

            var unpacker = new RR7Unpacker(options.InputPath, options.OutputPath);
            unpacker.ReadToc(options.GameCode, options.ElfPath);
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

            var unpacker = new RRNUnpacker(options.InputPath, options.OutputPath);
            unpacker.ReadToc(options.InfoPath);
            unpacker.ExtractContainers();
        }
    }



    [Verb("rr7", HelpText = "Unpacks .DAT files for Ridge Racer 7 (PS3).")]
    public class RR7Verbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RR7.DAT.")]
        public string InputPath { get; set; }

        [Option('e', "elf-path", Required = true, HelpText = "Input .info file that should be next to the DAT file.")]
        public string ElfPath { get; set; }

        [Option('g', "gamecode", Required = true, HelpText = "Game Code of the game. Example: NPEB00513")]
        public string GameCode { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }

    [Verb("rrn", HelpText = "Unpacks .DAT files for Ridge Racer (PS Vita).")]
    public class RRNVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input .DAT file like RRN.DAT.")]
        public string InputPath { get; set; }

        [Option("info", Required = true, HelpText = "Input decrypted .elf file for the game. Example: main.self.")]
        public string InfoPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output directory for the extracted files.")]
        public string OutputPath { get; set; }
    }
}
