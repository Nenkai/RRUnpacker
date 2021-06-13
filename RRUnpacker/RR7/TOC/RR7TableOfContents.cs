using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.RR7.TOC
{
    public class RR7TableOfContents
    {
        public const int TOC_ELF_OFFSET = 0x620128;
        public const int ELF_OFFSET_DIFF = 0xFB30000;

        public const int ContainerCount = 2_088;
        public const int FileCount = 12_810;

        public List<RR7FileDescriptor> FileDescriptors = new();
        public List<RR7ContainerDescriptor> ContainerDescriptors = new();

        private string _elfPath;

        public RR7TableOfContents(string elfPath)
        {
            _elfPath = elfPath;
        }

        public void Read()
        {
            using var fs = new FileStream(_elfPath, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Big);

            fs.Position = TOC_ELF_OFFSET;
            ReadFileDescriptors(bs);
            ReadContainerDescriptors(bs);
        }

        private void ReadContainerDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < ContainerCount; i++)
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
            for (int i = 0; i < FileCount; i++)
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
