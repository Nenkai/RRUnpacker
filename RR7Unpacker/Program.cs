using System;
using System.IO;

namespace RR7Unpacker
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("RR7Unpacker (RR7.DAT) by Nenkai#9075");
            Console.WriteLine();

            if (args.Length < 3)
            {
                Console.WriteLine("-- RR7Unpacker <decrypted_elf_path (main.elf)> <rr7.dat path> <output directory>");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine($"ELF file '{args[0]}' does not exist.");
                return;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"RR7.DAT file '{args[1]}' does not exist.");
                return;
            }

            var unpacker = new RR7Unpacker(args[1], args[2]);
            unpacker.ReadToc(args[0]);
            unpacker.ExtractContainers();
        }
    }
}
