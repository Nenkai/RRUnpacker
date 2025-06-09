using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.Entities
{
    public class RRContainerDescriptor
    {
        public long Offset { get; set; }

        public string Name { get; set; }
        public uint SectorOffset { get; set; }
        public uint SectorSize { get; set; }

        public ushort FileDescriptorEntryIndexStart { get; set; }
        public ushort FileDescriptorEntryIndexEnd { get; set; }
        public RRCompressionType CompressionType { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint PaddingSize { get; set; }

        public override string ToString()
        {
            return $"{Name} | SectorOffset: {SectorOffset:X8} | SectorSize: {SectorSize:X8} | Compressed: {CompressionType} | ZSize: {CompressedSize:X8} | Size: {UncompressedSize:X8}";
        }
    }

    public enum RRCompressionType
    {
        None = 0,
        RRLZ = 1,

        // Only seen in Go Vacation (Switch).
        // Uses the switch's zlib (1.2.8.f-NINTENDO-SDK-v1)
        Zlib = 2,
    }
}
