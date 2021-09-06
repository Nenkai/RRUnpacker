using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker
{
    public class RRContainerDescriptor
    {
        public long Offset { get; set; }

        public string Name { get; set; }
        public uint SectorOffset { get; set; }
        public ushort SectorSize { get; set; }

        public ushort FileDescriptorEntryIndexStart { get; set; }
        public ushort FileDescriptorEntryIndexEnd { get; set; }
        public bool Compressed { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint PaddingSize { get; set; }

        public override string ToString()
        {
            return $"{Name} | SectorOffset: {SectorOffset:X8} | SectorSize: {SectorSize:X8} | Compressed: {Compressed} | ZSize: {CompressedSize:X8} | Size: {UncompressedSize:X8}";
        }
    }
}
