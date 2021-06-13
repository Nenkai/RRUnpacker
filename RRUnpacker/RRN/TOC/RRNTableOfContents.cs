﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.RRN.TOC
{
    public class RRNTableOfContents
    {
        public List<RRNFileDescriptor> FileDescriptors = new();
        public List<RRNContainerDescriptor> ContainerDescriptors = new();

        public ulong ContainerFileSize { get; set; }
        public ulong ContainerTotalUncompressedSize { get; set; }

        private string _infoPath;

        public RRNTableOfContents(string infoPath)
        {
            _infoPath = infoPath;
        }

        public void Read()
        {
            using var textReader = File.OpenText(_infoPath);

            ReadTOC(textReader);
        }

        private void ReadTOC(StreamReader reader)
        {
            string str = reader.ReadLine();
            if (string.IsNullOrEmpty(str) || !ulong.TryParse(str, out ulong totalSize))
                throw new InvalidDataException("Info file is has incorrect or missing container file size on first line.");

            str = reader.ReadLine();
            if (string.IsNullOrEmpty(str) || !ulong.TryParse(str, out ulong totalUncompressedSize))
                throw new InvalidDataException("Info file is has incorrect or missing uncompressed container file size on second line.");

            ContainerFileSize = totalSize;
            ContainerTotalUncompressedSize = totalUncompressedSize;

            ReadContainerDescriptors(reader);
            ReadFileDescriptors(reader);
        }

        public void ReadContainerDescriptors(StreamReader reader)
        {
            string str = null;
            while (!reader.EndOfStream && (str = reader.ReadLine()) != string.Empty && str != null)
            {
                string[] spl = str.Split(' ');
                if (spl.Length != 9)
                {
                    Console.WriteLine($"Skipping line '{str}', expected 9 arguments got {spl.Length}.");
                    continue;
                }

                string containerName = spl[0];
                if (!uint.TryParse(spl[1], out uint sectorOffset))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container sector offset '{spl[1]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[2], out uint sectorSize))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container sector size '{spl[2]}'.");
                    continue;
                }

                if (!int.TryParse(spl[3], out int fileIndexStart))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container file index start '{spl[3]}'.");
                    continue;
                }

                if (!int.TryParse(spl[4], out int fileIndexEnd))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container file index end '{spl[4]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[5], out uint compressed))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container compressed flag '{spl[5]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[6], out uint containerCompressedSize))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container compressed size '{spl[6]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[7], out uint containerUncompressedSize))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container uncompressed size '{spl[7]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[8], out uint unk))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse unk (argument 9) '{spl[8]}'.");
                    continue;
                }

                RRNContainerDescriptor containerDescriptor = new RRNContainerDescriptor();
                containerDescriptor.Name = containerName;
                containerDescriptor.SectorOffset = sectorOffset;
                containerDescriptor.SectorSize = sectorSize;
                containerDescriptor.FileDescriptorEntryIndexStart = fileIndexStart;
                containerDescriptor.FileDescriptorEntryIndexEnd = fileIndexEnd;
                containerDescriptor.Compressed = compressed == 1;
                containerDescriptor.UncompressedSize = containerUncompressedSize;
                containerDescriptor.CompressedSize = containerCompressedSize;

                ContainerDescriptors.Add(containerDescriptor);
            }
        }

        public void ReadFileDescriptors(StreamReader reader)
        {
            string str = null;
            while (!reader.EndOfStream && (str = reader.ReadLine()) != string.Empty && str != null)
            {
                string[] spl = str.Split(' ');
                if (spl.Length != 6)
                {
                    Console.WriteLine($"Skipping line '{str}', expected 6 arguments got {spl.Length}.");
                    continue;
                }

                string containerName = spl[0];
                if (!uint.TryParse(spl[1], out uint sectorOffset))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container sector offset '{spl[1]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[2], out uint sectorSize))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse container sector size '{spl[2]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[3], out uint unkFlag))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse file flag '{spl[3]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[4], out uint fileSizeWithinContainer))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse file size '{spl[4]}'.");
                    continue;
                }

                if (!uint.TryParse(spl[5], out uint fileOffsetWithinContainer))
                {
                    Console.WriteLine($"Skipping line '{str}', could not parse file offset '{spl[5]}'.");
                    continue;
                }

                RRNFileDescriptor fileDescriptor = new RRNFileDescriptor();
                fileDescriptor.Name = containerName;
                fileDescriptor.SectorOffset = sectorOffset;
                fileDescriptor.SectorSize = sectorSize;
                fileDescriptor.UnkFlag = unkFlag;
                fileDescriptor.FileSizeWithinContainer = fileSizeWithinContainer;
                fileDescriptor.FileOffsetWithinContainer = fileOffsetWithinContainer;

                FileDescriptors.Add(fileDescriptor);
            }
        }
    }
}
