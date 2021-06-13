using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker.RR7
{
    public class RR7FileDescriptor
    {
        public string Name { get; set; }
        public uint OffsetWithinContainer { get; set; }
        public uint FileSizeWithinContainer { get; set; }
    }
}
