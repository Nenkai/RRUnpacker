using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using RRUnpacker.Entities;
using RRUnpacker.Headers;
using RRUnpacker.Decompressors;
using System.Runtime.CompilerServices;

namespace RRUnpacker.TOC; 

public record GoVacationTocInformation(string DataFileName, int ContainerCount, int ContainersTocOffset, string ContainersFileName, 
    int FileCount, int FileTocOffset, string FileNames, int DatSize);

/// <summary>
/// TOC Within the main.self executable for Go Vacation (NX).
/// </summary>
public class GoVacationWiiTableOfContents : ITableOfContents
{
    public static List<GoVacationTocInformation> TOCInfos =
    [
        // Offsets are into select rso module in BIN000.DAT.

        // Go Vacation (Wii) - Europe - SGVPAF
        new GoVacationTocInformation("DISC.DAT", 
            788, 0x3CFEC8, "SGVPAF_container_names.txt", 
            4859, 0x3D7D18, "SGVPAF_file_names.txt",
            0x3F2B7000), // Offset into select rso module in BIN000.DAT.

        // Go Vacation (Wii) (Prototype) - US/NTSC-U - SGVEAF 
        new GoVacationTocInformation("DISC.DAT",
            687, 0x406F68, "SGVEAF_proto_container_names.txt",
            4674, 0x40E244, "SGVEAF_proto_file_names.txt",
            0x2D00B800),
    ];

    public GoVacationTocInformation CurrentTOCInfo { get; set; }

    public List<RRFileDescriptor> FileDescriptors = [];
    public List<RRContainerDescriptor> ContainerDescriptors = [];

    private string _bin000Path;

    private List<string> _containerNames = [];
    private List<string> _fileNames = [];

    public GoVacationWiiTableOfContents(string inputPath, string bin000Path)
    {
        string fileName = Path.GetFileName(inputPath);
        long datSize = new FileInfo(inputPath).Length;
        GoVacationTocInformation toc = TOCInfos.FirstOrDefault(e => e.DataFileName == fileName && e.DatSize == datSize) 
            ?? throw new NotSupportedException("Data file is not supported. (Every different DISC.DAT file needs to be manually implemented).");

        CurrentTOCInfo = toc;
        _bin000Path = bin000Path;
    }

    public void Read()
    {
        _containerNames = File.ReadAllLines(Path.Combine("TOC", "FileNames", CurrentTOCInfo.ContainersFileName)).ToList();
        _fileNames = File.ReadAllLines(Path.Combine("TOC", "FileNames", CurrentTOCInfo.FileNames)).ToList();

        U8Archive u8 = U8Archive.Open(_bin000Path);

        // Decompress select rso module. (This is the one that is used. select/main/villa/kigae also store a copy of the toc)
        byte[] staticBytes = u8.GetFile("select");
        if (!RRLZDecompressor.DecompressWithHeader(staticBytes, out byte[]? decompressedSelectData))
            throw new InvalidDataException("Failed to decompress 'select' rso module from BIN000.dat.");

        // We are doing a mix of reading the toc from the rso but also reading file names dumped manually. Why?
        // Because name pointers are blanked out in the rso since they're awaiting relocation.
        // Ideally, we should support relocation, but that's a quite some work. So:

        // TODO: Support relocation so we can read name pointers properly from the rso.

        // Oh also if you wonder how I got the names out, I simply dumped MRAM through dolphin after select was loaded.
        // The toc then contains pointers.

        using var fs = new MemoryStream(decompressedSelectData);
        using var bs = new BinaryStream(fs, ByteConverter.Big);

        // This time it's the other way around, containers are first
        fs.Position = CurrentTOCInfo.ContainersTocOffset;
        ReadContainerDescriptors(bs);

        fs.Position = CurrentTOCInfo.FileTocOffset;
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
            desc.Name = _containerNames[i];
            desc.SectorOffset = bs.ReadUInt32();
            desc.SectorSize = bs.ReadUInt16();
            desc.FileDescriptorEntryIndexStart = bs.ReadUInt16();
            desc.FileDescriptorEntryIndexEnd = bs.ReadUInt16();
            desc.CompressionType = (RRCompressionType)bs.ReadUInt16();
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

            uint nameOffset = bs.ReadUInt32();
            desc.Name = _fileNames[i];
            uint globalSectorOffset = bs.ReadUInt32(); // int - basically when all assets are decompressed?
            ushort numSectors = bs.ReadUInt16();
            ushort alignment = bs.ReadUInt16();
            desc.FileSizeWithinContainer = bs.ReadUInt32();
            desc.OffsetWithinContainer = bs.ReadUInt32();

            FileDescriptors.Add(desc);
        }
    }
}
