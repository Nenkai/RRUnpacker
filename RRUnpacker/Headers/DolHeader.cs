using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Syroot.BinaryData;

namespace RRUnpacker.Headers;

public class DolHeader
{
    public uint[] SectionFileOffsets { get; set; } = new uint[18];
    public uint[] SectionRelocAdr { get; set; } = new uint[18];
    public uint[] SectionLengths { get; set; } = new uint[18];
    public uint BssSecAddress { get; set; }
    public uint BssSecLength { get; set; }
    public uint EntryPoint { get; set; }

    public static DolHeader ReadHeader(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        return ReadHeader(fs);
    }

    public static DolHeader ReadHeader(Stream stream)
    {
        var br = new BinaryStream(stream, ByteConverter.Big);

        var dol = new DolHeader();
        for (int i = 0; i < 18; i++)
            dol.SectionFileOffsets[i] = br.ReadUInt32();

        for (int i = 0; i < 18; i++)
            dol.SectionRelocAdr[i] = br.ReadUInt32();

        for (int i = 0; i < 18; i++)
            dol.SectionLengths[i] = br.ReadUInt32();

        // Read BSS section address and length
        dol.BssSecAddress = br.ReadUInt32();
        dol.BssSecLength = br.ReadUInt32();
        // Read entry point
        dol.EntryPoint = br.ReadUInt32();
        return dol;
    }
    public uint? GetExecutableAddressForVirtualAddress(uint virtualAddress)
    {
        for (int i = 0; i < SectionFileOffsets.Length; i++)
        {
            if (SectionRelocAdr[i] <= virtualAddress && virtualAddress < SectionRelocAdr[i] + SectionLengths[i])
                return SectionFileOffsets[i] + (virtualAddress - SectionRelocAdr[i]);
        }

        return null;
    }
}
