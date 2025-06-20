using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
    public interface IRRDBCell
    {
        public void Serialize(BinaryStream bs);
    }
}
