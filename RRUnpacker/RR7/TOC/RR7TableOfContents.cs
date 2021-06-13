using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.RR7.TOC
{
    public record TOCInformation (int FileCount, int ContainerCount, int TOCOffset);

    public class RR7TableOfContents
    {
        public static Dictionary<string, TOCInformation> TOCInfos = new()
        {
            { "NPUB30457", new TOCInformation(2_088, 12_810, 0x620128) },
            { "NPEB00513", new TOCInformation(2_139, 13_313, 0x630128) },

        };

        public TOCInformation CurrentTOCInfo { get; set; }

        public const int ELF_OFFSET_DIFF = 0xFB20000;

        public List<RR7FileDescriptor> FileDescriptors = new();
        public List<RR7ContainerDescriptor> ContainerDescriptors = new();

        private string _elfPath;

        public RR7TableOfContents(string gameCode, string elfPath)
        {
            if (!TOCInfos.TryGetValue(gameCode, out TOCInformation toc))
                throw new ArgumentException("Invalid or non-supported game code provided.");

            CurrentTOCInfo = toc;
            _elfPath = elfPath;
        }

        public void Read()
        {
            using var fs = new FileStream(_elfPath, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Big);

            fs.Position = CurrentTOCInfo.TOCOffset;
            ReadFileDescriptors(bs);
            ReadContainerDescriptors(bs);
        }

        private void ReadContainerDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < CurrentTOCInfo.ContainerCount; i++)
            {
                RR7ContainerDescriptor desc = new RR7ContainerDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - ELF_OFFSET_DIFF, SeekOrigin.Begin))
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
                RR7FileDescriptor desc = new RR7FileDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - ELF_OFFSET_DIFF, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                bs.Position += 8;
                desc.FileSizeWithinContainer = bs.ReadUInt32();
                desc.OffsetWithinContainer = bs.ReadUInt32();

                FileDescriptors.Add(desc);
            }
        }
    }
}
