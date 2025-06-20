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
/// TOC Within the main.self executable for Go Vacation (NX).
/// </summary>
public class GoVacationNXTableOfContents : ITableOfContents
{
    public static Dictionary<string, TOCInformation> TOCInfos = new()
    {
        { "DISC.DAT", new TOCInformation(6000, 944, 0x5610F82, -0xD8, 0) }, // Allocated Limits: 6400, 1600?
        { "RIZ.DAT", new TOCInformation(1466, 10, 0x56505CA, -0xD8, 0) }, // Allocated Limits: 2048, 10?
        { "SHD.DAT", new TOCInformation(77, 9, 0x5686B8A, -0xD8, 0) } // Allocated Limits: 128, 10?
    };

    public TOCInformation CurrentTOCInfo { get; set; }

    public List<RRFileDescriptor> FileDescriptors = new();
    public List<RRContainerDescriptor> ContainerDescriptors = new();

    private string _elfPath;

    public GoVacationNXTableOfContents(string inputPath, string elfPath)
    {
        string fileName = Path.GetFileName(inputPath);
        if (!TOCInfos.TryGetValue(fileName, out TOCInformation? toc))
            throw new ArgumentException("TOC for file was not found. Make sure that the file has not been renamed (i.e DISC.DAT/RIZ.DAT/SHD.DAT).");

        CurrentTOCInfo = toc;
        _elfPath = elfPath;
    }

    public void Read()
    {
        using var fs = new FileStream(_elfPath, FileMode.Open);
        using var bs = new BinaryStream(fs, ByteConverter.Little);

        fs.Position = CurrentTOCInfo.TOCOffset;

        // This time it's the other way around, containers are first
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
            desc.Offset = bs.Position;

            long nameOffset = bs.ReadInt64();
            using (var seek = bs.TemporarySeek(nameOffset - CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            desc.SectorOffset = bs.ReadUInt32();
            desc.SectorSize = bs.ReadUInt32();
            desc.FileDescriptorEntryIndexStart = bs.ReadUInt16();
            desc.FileDescriptorEntryIndexEnd = bs.ReadUInt16();
            desc.CompressionType = (RRCompressionType)bs.ReadUInt32();
            desc.CompressedSize = bs.ReadUInt32();
            desc.UncompressedSize = bs.ReadUInt32();
            desc.PaddingSize = bs.ReadUInt32();

            ContainerDescriptors.Add(desc);
            bs.Position += 0x04;
        }
    }

    private void ReadFileDescriptors(BinaryStream bs)
    {
        for (int i = 0; i < CurrentTOCInfo.FileCount; i++)
        {
            RRFileDescriptor desc = new RRFileDescriptor();
            desc.Offset = bs.Position;

            long nameOffset = bs.ReadInt64();
            using (var seek = bs.TemporarySeek(nameOffset - CurrentTOCInfo.ELFOffsetDiff, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            // TODO.
            uint globalSectorOffset = bs.ReadUInt32(); // int - global SectorOffset? basically when all assets are decompressed?
            uint numSectors = bs.ReadUInt32(); // int - NumSectors?
            uint alignment = bs.ReadUInt32(); // int - alignment?
            desc.FileSizeWithinContainer = bs.ReadUInt32();
            desc.OffsetWithinContainer = bs.ReadUInt32();
            bs.Position += 4;

            FileDescriptors.Add(desc);
        }
    }
}
