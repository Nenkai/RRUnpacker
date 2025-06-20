using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData;

namespace RR.Files.Database.Types
{
	public class RRDBFloat : IRRDBCell
	{
		public float Value { get; set; }
		public RRDBFloat(float val)
		{
			Value = val;
		}

		public void Serialize(BinaryStream bs)
			=> bs.WriteSingle(Value);

		public override string ToString()
			=> Value.ToString();
    }
}
