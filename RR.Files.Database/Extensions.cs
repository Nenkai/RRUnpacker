using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database
{
    public static class Extensions
    {
        public static void AlignWithValue(this BinaryStream bs, byte value, uint alignment)
        {
            while (bs.Position % alignment != 0)
                bs.WriteByte(value);
        }
    }
}
