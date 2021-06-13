using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.RR6
{
    public class RR6FileDescriptor
    {
        public string Name { get; set; }
        public uint OffsetWithinContainer { get; set; }
        public uint FileSizeWithinContainer { get; set; }
    }
}
