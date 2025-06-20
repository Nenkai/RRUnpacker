using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RR.Files.Database
{
	public class RRDBColumnInfo
	{
		public int RowColumnOffset { get; set; }
		public int RowNameOffset { get; set; }
		public string Name { get; set; }
		public RRDBColumnType Type { get; set; }

		public int GetTypeSize()
		{
			return Type switch
			{
				RRDBColumnType.Byte => sizeof(byte),
				RRDBColumnType.Float => sizeof(float),
				RRDBColumnType.Short => sizeof(short),
				RRDBColumnType.Integer => sizeof(int),
				RRDBColumnType.String => sizeof(int),
				_ => throw new Exception("Invalid column type"),
			};
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public enum RRDBColumnType
	{
		Byte = 1,
		Short = 2,
		Integer = 3,
		Float = 4,
		String = 5,
	}
}
