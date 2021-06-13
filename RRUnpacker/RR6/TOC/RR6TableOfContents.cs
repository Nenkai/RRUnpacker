using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.RR6.TOC
{
    public class RR6TableOfContents
    {
        // Within XEX
        public const int TOC_OFFSET = 0x339698;
        public const uint XEX_OFFSET_DIFF = 0x81FFE000;

        // RRM1.DAT
        public const int RRM1ContainerCount = 431;
        public const int RRM1FileCount = 1277;
        public List<RR6FileDescriptor> FileDescriptors1;
        public List<RR6ContainerDescriptor> ContainerDescriptors1;

        // RRM2.DAT
        public const int RRM2ContainerCount = 60;
        public const int RRM2FileCount = 550;
        public List<RR6FileDescriptor> FileDescriptors2;
        public List<RR6ContainerDescriptor> ContainerDescriptors2;

        // RRM3.DAT
        public const int RRM3ContainerCount = 1224;
        public const int RRM3FileCount = 1465;
        public List<RR6FileDescriptor> FileDescriptors3;
        public List<RR6ContainerDescriptor> ContainerDescriptors3;

        private string _elfPath;

        public RR6TableOfContents(string elfPath)
        {
            _elfPath = elfPath;
        }

        public void Read()
        {
            using var fs = new FileStream(_elfPath, FileMode.Open);
            using var bs = new BinaryStream(fs, ByteConverter.Big);

            fs.Position = TOC_OFFSET;
            ContainerDescriptors1 = ReadContainerDescriptors(bs, RRM1ContainerCount);
            FileDescriptors1 = ReadFileDescriptors(bs, RRM1FileCount);

            ContainerDescriptors2 = ReadContainerDescriptors(bs, RRM2ContainerCount);
            FileDescriptors2 = ReadFileDescriptors(bs, RRM2FileCount);

            ContainerDescriptors3 = ReadContainerDescriptors(bs, RRM3ContainerCount);
            FileDescriptors3 = ReadFileDescriptors(bs, RRM3FileCount);

        }

        public List<RR6ContainerDescriptor> GetContainerTOCByFileName(string fileName)
        {
            return fileName switch
            {
                "RRM" => ContainerDescriptors1,
                "RRM2" => ContainerDescriptors2,
                "RRM3" => ContainerDescriptors3,
                _ => null,
            };
        }

        public List<RR6FileDescriptor> GetFileTOCByFileName(string fileName)
        {
            return fileName switch
            {
                "RRM" => FileDescriptors1,
                "RRM2" => FileDescriptors2,
                "RRM3" => FileDescriptors3,
                _ => null,
            };
        }

        private List<RR6ContainerDescriptor> ReadContainerDescriptors(BinaryStream bs, int size)
        {
            var list = new List<RR6ContainerDescriptor>();
            for (int i = 0; i < size; i++)
            {
                RR6ContainerDescriptor desc = new RR6ContainerDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - XEX_OFFSET_DIFF, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                desc.SectorOffset = bs.ReadUInt32();
                desc.SectorSize = bs.ReadUInt16();
                desc.FileDescriptorEntryIndexStart = bs.ReadUInt16();
                desc.FileDescriptorEntryIndexEnd = bs.ReadUInt16();
                desc.Compressed = bs.ReadBoolean(BooleanCoding.Word);
                desc.CompressedSize = bs.ReadUInt32();
                desc.UncompressedSize = bs.ReadUInt32();
                desc.PaddingSize = bs.ReadUInt32();

                list.Add(desc);
                bs.Position += 8;
            }

            bs.Align(0x08);
            return list;
        }

        private List<RR6FileDescriptor> ReadFileDescriptors(BinaryStream bs, int size)
        {
            var list = new List<RR6FileDescriptor>();
            for (int i = 0; i < size; i++)
            {
                RR6FileDescriptor desc = new RR6FileDescriptor();

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - XEX_OFFSET_DIFF, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                bs.Position += 8;
                desc.FileSizeWithinContainer = bs.ReadUInt32();
                desc.OffsetWithinContainer = bs.ReadUInt32();

                list.Add(desc);
            }

            bs.Align(0x08);
            return list;
        }
    }
}
