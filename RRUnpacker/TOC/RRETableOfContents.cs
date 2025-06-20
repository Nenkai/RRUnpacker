using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using RRUnpacker.Entities;

namespace RRUnpacker.TOC;

/// <summary>
/// TOC Within the PS2 executable for R:Racing.
/// </summary>
public class RRETableOfContents : ITableOfContents
{
    // Within SLES_52309
    public const int TOC_OFFSET = 0x399EC0;
    public const uint ELF_OFFSET_DIFF = 0xFF000;

    public const int ContainerCount = 3320;
    public const int FileCount = 5233;
    public List<RRFileDescriptor> FileDescriptors;
    public List<RRContainerDescriptor> ContainerDescriptors;


    private string _elfPath;

    public RRETableOfContents(string elfPath)
    {
        _elfPath = elfPath;
    }

    public void Read()
    {
        using var fs = new FileStream(_elfPath, FileMode.Open);
        using var bs = new BinaryStream(fs, ByteConverter.Little);

        fs.Position = TOC_OFFSET;
        ContainerDescriptors = ReadContainerDescriptors(bs, ContainerCount);
        FileDescriptors = ReadFileDescriptors(bs, FileCount);
    }

    public List<RRContainerDescriptor> GetContainers(string fileName)
        => ContainerDescriptors;

    public List<RRFileDescriptor> GetFiles(string fileName)
        => FileDescriptors;

    private List<RRContainerDescriptor> ReadContainerDescriptors(BinaryStream bs, int size)
    {
        var list = new List<RRContainerDescriptor>();
        for (int i = 0; i < size; i++)
        {
            RRContainerDescriptor desc = new RRContainerDescriptor();

            uint nameOffset = bs.ReadUInt32();
            using (var seek = bs.TemporarySeek(nameOffset - ELF_OFFSET_DIFF, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            // Shorts are ints
            desc.SectorOffset = bs.ReadUInt32();
            desc.SectorSize = (ushort)bs.ReadUInt32();
            desc.FileDescriptorEntryIndexStart = (ushort)bs.ReadUInt32();
            desc.FileDescriptorEntryIndexEnd = (ushort)bs.ReadUInt32();
            desc.CompressionType = (RRCompressionType)bs.ReadUInt16();
            desc.CompressedSize = bs.ReadUInt32();
            desc.UncompressedSize = bs.ReadUInt32();
            desc.PaddingSize = bs.ReadUInt32();

            list.Add(desc);
            bs.Position += 8;
        }

        return list;
    }

    private List<RRFileDescriptor> ReadFileDescriptors(BinaryStream bs, int size)
    {
        var list = new List<RRFileDescriptor>();
        for (int i = 0; i < size; i++)
        {
            RRFileDescriptor desc = new RRFileDescriptor();

            uint nameOffset = bs.ReadUInt32();
            using (var seek = bs.TemporarySeek(nameOffset - ELF_OFFSET_DIFF, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            // Shorts are ints
            bs.Position += 12;
            desc.FileSizeWithinContainer = bs.ReadUInt32();
            desc.OffsetWithinContainer = bs.ReadUInt32();
            bs.Position += 4;
            list.Add(desc);
        }

        // Elf struct entries aligned on a 0x08
        return list;
    }
}
