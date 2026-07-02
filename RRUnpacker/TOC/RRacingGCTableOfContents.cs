// Copyright (c) 2026 Nenkai
// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RRUnpacker.Entities;
using RRUnpacker.Headers;

using Syroot.BinaryData;

namespace RRUnpacker.TOC; 


/// <summary>
/// TOC Within the main.dol executable.
/// </summary>
public class RRacingGCTableOfContents : ITableOfContents
{
    public record TOCInformation2(string FileName, int ContainerCount, uint ContainersTocVirtualOffset, int FileCount, uint FilesTocVirtualOffset, uint DatSize);

    public static List<TOCInformation2> TOCInfos =
    [
        // R: RACING (Europe) (GRJP69)
        new TOCInformation2("RGC.DAT", 3187, 0x80317928, 4960, 0x80339cec, 0x12EA1080),

        // R: RACING Evolution (USA) (GRJEAF)
        new TOCInformation2("RGCUSA.DAT", 2900, 0x803127f8, 4494, 0x80331a68, 0xAEB7800),

        // R:RACING EVOLUTION (Japan) (GRJJAF)
        new TOCInformation2("RGC.DAT", 2900, 0x80312218, 4492, 0x80331488, 0xB6CD800),
    ];

    public TOCInformation2 CurrentTOCInfo { get; set; }

    public List<RRFileDescriptor> FileDescriptors = [];
    public List<RRContainerDescriptor> ContainerDescriptors = [];

    private string _dolPath;
    private DolHeader _dolHeader;

    public RRacingGCTableOfContents(string inputPath, string dolPath)
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
        // we need to calculate virtual offsets as segments are relocated and not continuous (which is problematic for reading straight from the executable)

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
            desc.SectorSize = (ushort)bs.ReadUInt32();
            desc.FileDescriptorEntryIndexStart = (ushort)bs.ReadUInt32();
            desc.FileDescriptorEntryIndexEnd = (ushort)bs.ReadUInt32();
            desc.CompressionType = (RRCompressionType)bs.ReadUInt32();
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

            bs.Position += 12;
            desc.FileSizeWithinContainer = bs.ReadUInt32();
            desc.OffsetWithinContainer = bs.ReadUInt32();
            bs.Position += 4;
            FileDescriptors.Add(desc);
        }
    }
}
