using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
	public class RRDBString : IRRDBCell
	{
		public string Value { get; set; }
		public int StringOffset { get; set; }

		public RRDBString(string val)
		{
			Value = val;
		}

		public void Serialize(BinaryStream bs)
			=> bs.WriteInt32(StringOffset);

		public override string ToString()
			=> Value;
    }
}
