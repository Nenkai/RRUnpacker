using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
	public class RRDBShort : IRRDBCell
	{
		public short Value { get; set; }
		public RRDBShort(short val)
		{
			Value = val;
		}

		public void Serialize(BinaryStream bs)
			=> bs.WriteInt16(Value);

		public override string ToString()
			=> Value.ToString();
	}
}
