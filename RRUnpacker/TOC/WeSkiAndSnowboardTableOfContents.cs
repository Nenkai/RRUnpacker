using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using RRUnpacker.Entities;
using RRUnpacker.Headers;

namespace RRUnpacker.TOC; 

public record TOCInformation2(string FileName, int ContainerCount, uint ContainersTocVirtualOffset, int FileCount, uint FilesTocVirtualOffset, uint DatSize);

/// <summary>
/// TOC Within the main.self executable for Go Vacation (NX).
/// </summary>
public class WeSkiAndSnowboardTableOfContents : ITableOfContents
{
    public static List<TOCInformation2> TOCInfos =
    [
        // We Ski & Snowboard (Prototype) - RYKEAF - dol date 8/19/2008 5:34:00 PM
        new TOCInformation2("SKI.DAT", 359, 0x802ed9a0, 2619, 0x802f1560, 0x2B67E000), // Allocated Limits: 12888, 1536?

        // We Ski (Prototype) - RSQEAF - dol date 12/10/2007 3:37:00 PM
        new TOCInformation2("SKI.DAT", 235, 0x802c0560, 1562, 0x802c2db0, 0x16FBF000), // Allocated Limits: 12888, 1536?
    ];

    public TOCInformation2 CurrentTOCInfo { get; set; }

    public List<RRFileDescriptor> FileDescriptors = [];
    public List<RRContainerDescriptor> ContainerDescriptors = [];

    private string _dolPath;
    private DolHeader _dolHeader;

    public WeSkiAndSnowboardTableOfContents(string inputPath, string dolPath)
    {
        string fileName = Path.GetFileName(inputPath);
        long datSize = new FileInfo(inputPath).Length;
        TOCInformation2 toc = TOCInfos.FirstOrDefault(e => e.FileName == fileName && e.DatSize == datSize)
            ?? throw new NotSupportedException("Data file is not supported. (Every different SKI.DAT file needs to be manually implemented).");

        CurrentTOCInfo = toc;
        _dolPath = dolPath;
    }

    public void Read()
    {
        using var fs = new FileStream(_dolPath, FileMode.Open);
        using var bs = new BinaryStream(fs, ByteConverter.Big);
        _dolHeader = DolHeader.ReadHeader(fs);

        // This time it's the other way around, containers are first
        // In We Ski we need to calculate virtual offsets as segments are relocated and not continuous (which is problematic for reading straight from the executable)

        uint? containersTocOffset = _dolHeader.GetExecutableAddressForVirtualAddress(CurrentTOCInfo.ContainersTocVirtualOffset)
            ?? throw new InvalidDataException($"Could not find executable address for containers toc virtual offset {CurrentTOCInfo.ContainersTocVirtualOffset:X8}");
        fs.Position = (long)containersTocOffset;
        ReadContainerDescriptors(bs);

        uint? filesTocOffset = _dolHeader.GetExecutableAddressForVirtualAddress(CurrentTOCInfo.FilesTocVirtualOffset)
            ?? throw new InvalidDataException($"Could not find executable address for containers toc virtual offset {CurrentTOCInfo.FilesTocVirtualOffset:X8}");
        fs.Position = (long)filesTocOffset;
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

            uint nameOffset = bs.ReadUInt32();
            uint? executableAddress = _dolHeader.GetExecutableAddressForVirtualAddress(nameOffset) 
                ?? throw new InvalidDataException($"Container {i}: could not find executable address for name offset {nameOffset:X8}");

            using (var seek = bs.TemporarySeek((long)executableAddress, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            desc.SectorOffset = bs.ReadUInt32();
            desc.SectorSize = bs.ReadUInt16();
            desc.FileDescriptorEntryIndexStart = bs.ReadUInt16();
            desc.FileDescriptorEntryIndexEnd = bs.ReadUInt16();
            desc.CompressionType = (RRCompressionType)bs.ReadUInt16();
            desc.CompressedSize = bs.ReadUInt32();
            desc.UncompressedSize = bs.ReadUInt32();
            desc.PaddingSize = bs.ReadUInt32();

            ContainerDescriptors.Add(desc);
            bs.Position += 0x08;
        }
    }

    private void ReadFileDescriptors(BinaryStream bs)
    {
        for (int i = 0; i < CurrentTOCInfo.FileCount; i++)
        {
            RRFileDescriptor desc = new RRFileDescriptor();
            desc.Offset = bs.Position;

            uint nameOffset = bs.ReadUInt32();
            uint? executableAddress = _dolHeader.GetExecutableAddressForVirtualAddress(nameOffset)
                ?? throw new InvalidDataException($"File {i}: could not find executable address for name offset {nameOffset:X8}");
            using (var seek = bs.TemporarySeek((long)executableAddress, SeekOrigin.Begin))
                desc.Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);

            // TODO.
            uint globalSectorOffset = bs.ReadUInt32(); // int - basically when all assets are decompressed?
            ushort numSectors = bs.ReadUInt16();
            ushort alignment = bs.ReadUInt16();
            desc.FileSizeWithinContainer = bs.ReadUInt32();
            desc.OffsetWithinContainer = bs.ReadUInt32();

            FileDescriptors.Add(desc);
        }
    }
}
