using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.TOC
{
    /// <summary>
    /// TOC Within the BOOT.bin executable for RR PSP (Version 1 and Version 2).
    /// </summary>
    public class RRPTableOfContents : ITableOfContents
    {
        public static Dictionary<string, TOCInformation> TOCInfos = new()
        {
            { "UCES00422", new TOCInformation(4_247, 1_651, 0x1C754C, 0xC0) }, // Ridge Racer 2  (PAL)
            { "ULJS00080", new TOCInformation(3_400, 1_487, 0x1C694C, 0xC0) }, // Ridge Racers 2 (JP)
            { "ULJS00001", new TOCInformation(2_632, 0_716, 0x1B6914, 0x80) }, // Ridge Racers   (JP)

        };

        public TOCInformation CurrentTOCInfo { get; set; }

        public List<RRFileDescriptor> FileDescriptors = new();
        public List<RRContainerDescriptor> ContainerDescriptors = new();

        private string _elfPath;

        public RRPTableOfContents(string gameCode, string elfPath)
        {
            if (!TOCInfos.TryGetValue(gameCode, out TOCInformation toc))
                throw new ArgumentException("Invalid or non-supported game code provided.");

            CurrentTOCInfo = toc;
            _elfPath = elfPath;
        }

        public void Read()
        {
            using var fs = new FileStream(_elfPath, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Little);

            fs.Position = CurrentTOCInfo.TOCOffset;
            ReadContainerDescriptors(bs);
            ReadFileDescriptors(bs);
        }

        public List<RRFileDescriptor> GetFiles(string fileName)
            => FileDescriptors;

        public List<RRContainerDescriptor> GetContainers(string fileName)
            => ContainerDescriptors;

        private void ReadContainerDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < CurrentTOCInfo.ContainerCount; i++)
            {
                RRContainerDescriptor desc = new RRContainerDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset + CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                desc.SectorOffset = bs.ReadUInt32();
                desc.SectorSize = bs.ReadUInt16();
                desc.FileDescriptorEntryIndexStart = bs.ReadUInt16();
                desc.FileDescriptorEntryIndexEnd = bs.ReadUInt16();
                desc.Compressed = bs.ReadBoolean(BooleanCoding.Word);
                desc.CompressedSize = bs.ReadUInt32();
                desc.UncompressedSize = bs.ReadUInt32();
                desc.PaddingSize = bs.ReadUInt32();

                ContainerDescriptors.Add(desc);
                bs.Position += 8;

            }
        }

        private void ReadFileDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < CurrentTOCInfo.FileCount; i++)
            {
                RRFileDescriptor desc = new RRFileDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset + CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                bs.Position += 8;
                desc.FileSizeWithinContainer = bs.ReadUInt32();
                desc.OffsetWithinContainer = bs.ReadUInt32();

                // Extra padding for RR PSP
                bs.Position += 4;
                FileDescriptors.Add(desc);
            }
        }
    }
}
