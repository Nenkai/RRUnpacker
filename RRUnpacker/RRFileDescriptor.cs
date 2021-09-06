using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RRUnpacker
{
    public class RRFileDescriptor
    {
        public long Offset { get; set; }

        public string Name { get; set; }
        public uint OffsetWithinContainer { get; set; }
        public uint FileSizeWithinContainer { get; set; }

        public override string ToString()
        {
            return $"{Name} | ContainerOffset: {OffsetWithinContainer:X8} | Size: {FileSizeWithinContainer:X8}";
        }
    }
}
