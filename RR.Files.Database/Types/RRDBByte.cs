using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
	public class RRDBByte : IRRDBCell
	{
		public byte Value { get; set; }
		public RRDBByte(byte val)
		{
			Value = val;
		}

		public void Serialize(BinaryStream bs)
			=> bs.WriteByte(Value);

		public override string ToString()
			=> Value.ToString();
	}
}
