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
    /// TOC Within the BOOT.bin executable for RR PSP (Version 2).
    /// </summary>
    public class RRPTableOfContents : ITableOfContents
    {
        public const int ELF_OFFSET_DIFF = 0xC0;
        public const uint TOC_OFFSET = 0x1C754C;

        public const int ContainerCount = 1_651;
        public const int FileCount = 4_097;

        public List<RRFileDescriptor> FileDescriptors = new();
        public List<RRContainerDescriptor> ContainerDescriptors = new();

        private string _elfPath;

        public RRPTableOfContents(string elfPath)
        {
            _elfPath = elfPath;
        }

        public void Read()
        {
            using var fs = new FileStream(_elfPath, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Little);

            fs.Position = TOC_OFFSET;
            ReadContainerDescriptors(bs);
            ReadFileDescriptors(bs);
        }

        public List<RRFileDescriptor> GetFiles(string fileName)
            => FileDescriptors;

        public List<RRContainerDescriptor> GetContainers(string fileName)
            => ContainerDescriptors;

        private void ReadContainerDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < ContainerCount; i++)
            {
                RRContainerDescriptor desc = new RRContainerDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset + ELF_OFFSET_DIFF, SeekOrigin.Begin))
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
                RRFileDescriptor desc = new RRFileDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset + ELF_OFFSET_DIFF, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                bs.Position += 8;
                desc.FileSizeWithinContainer = bs.ReadUInt32();
                desc.OffsetWithinContainer = bs.ReadUInt32();

                // Extra padding for RR PSP V2
                bs.Position += 4;
                FileDescriptors.Add(desc);
            }
        }
    }
}
