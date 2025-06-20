using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RR.Files.Database.Types;

namespace RR.Files.Database
{
    public class RRDBRowData
    {
        public List<IRRDBCell> Cells { get; set; } = new List<IRRDBCell>();
    }
}
