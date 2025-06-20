using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
	public class RRDBInt : IRRDBCell
	{
		public int Value { get; set; }
		public RRDBInt(int val)
		{
			Value = val;
		}

		public void Serialize(BinaryStream bs)
			=> bs.WriteInt32(Value);

		public override string ToString()
			=> Value.ToString();
	}
}
