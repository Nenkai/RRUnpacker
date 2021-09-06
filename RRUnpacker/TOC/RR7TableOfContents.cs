using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;

namespace RRUnpacker.TOC
{
    public record TOCInformation (int FileCount, int ContainerCount, int TOCOffset, long ELFOffsetDiff);

    /// <summary>
    /// TOC Within the main.self executable for RR7.
    /// </summary>
    public class RR7TableOfContents : ITableOfContents
    {
        public static Dictionary<string, TOCInformation> TOCInfos = new()
        {
            { "NPUB30457", new TOCInformation(12_810, 2_088, 0x620128, 0xFB30000) },
            { "NPEB00513", new TOCInformation(13_313, 2_139, 0x630128, 0xFB20000) },

        };

        public TOCInformation CurrentTOCInfo { get; set; }


        public List<RRFileDescriptor> FileDescriptors = new();
        public List<RRContainerDescriptor> ContainerDescriptors = new();

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

        public List<RRFileDescriptor> GetFiles(string fileName)
            => FileDescriptors;

        public List<RRContainerDescriptor> GetContainers(string fileName)
            => ContainerDescriptors;

        private void ReadContainerDescriptors(BinaryStream bs)
        {
            for (int i = 0; i < CurrentTOCInfo.ContainerCount; i++)
            {
                RRContainerDescriptor desc = new RRContainerDescriptor();
                desc.Offset = bs.Position;

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
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
                desc.Offset = bs.Position;

                uint nameOffset = bs.ReadUInt32();
                using (var seek = bs.TemporarySeek(nameOffset - CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
                    desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

                bs.Position += 8;
                desc.FileSizeWithinContainer = bs.ReadUInt32();
                desc.OffsetWithinContainer = bs.ReadUInt32();

                FileDescriptors.Add(desc);
            }
        }
    }
}
