using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.RRN.TOC
{
    public class RRNFileDescriptor
    {
        public string Name { get; set; }
        public uint SectorOffset { get; set; }
        public uint SectorSize { get; set; }
        public uint UnkFlag { get; set; }

        public uint FileSizeWithinContainer { get; set; }
        public uint FileOffsetWithinContainer { get; set; }
    }
}
