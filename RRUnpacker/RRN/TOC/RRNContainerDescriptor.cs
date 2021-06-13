using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.RRN.TOC
{
    public class RRNContainerDescriptor
    {
        public string Name { get; set; }
        public uint SectorOffset { get; set; }
        public uint SectorSize { get; set; }

        public int FileDescriptorEntryIndexStart { get; set; }
        public int FileDescriptorEntryIndexEnd { get; set; }
        public bool Compressed { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint PaddingSize { get; set; }
    }
}
