using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Syroot.BinaryData;
using RR.Files.Database.Types;

namespace RR.Files.Database;

public class Table
{
	public List<RRDBColumnInfo> Columns = [];
	public List<RRDBRowData> Rows = [];

	private record TableConst(string TableName, byte nMajorID, byte Unk);

	private static readonly List<TableConst> TableConsts =
    [
        new("Race", 1, 1),
		new("Car", 2, 1),
		new("Course", 3, 1),
		new("Parts", 4, 0),
		new("Custom", 5, 0),
		new("Prize", 6, 1),
		new("Country", 7, 0),
		new("WCNews", 8, 1),
		new("WCNewsTGS", 8, 1),
		new("DjFlag", 9, 0),
		new("Bgm", 10, 1),
		new("CarViewer", 11, 1),
		new("MenuBgm", 12, 0),
	];

	public string Name { get; set; }

	private string _path;

	public byte nMajorID { get; set; } // Table ID
	public Table()
	{

	}

	public Table(string path)
	{
		_path = path;
		Name = Path.GetFileNameWithoutExtension(path);
	}

	public void Read()
	{
		using var fs = new FileStream(_path, FileMode.Open);
		using var bs = new BinaryStream(fs);

		// Endian byte
		bs.ByteConverter = bs.ReadByte() == 1 ? ByteConverter.Big : ByteConverter.Little;

		nMajorID = bs.Read1Byte();
		bs.ReadByte();
		int columnCount = bs.ReadByte();

		int rowCount = bs.ReadInt32();
		int rowDataStartOffset = bs.ReadInt32();
		int rowLengthPadded = bs.ReadInt32();

		// Read column defs
		for (int i = 0; i < columnCount; i++)
		{
			var col = new RRDBColumnInfo();
			col.RowColumnOffset = bs.ReadInt32();
			Columns.Add(col);
		}

		// Read column names
		for (int i = 0; i < columnCount; i++)
		{
			int colNameOffset = bs.ReadInt32();
			using (var seek = bs.TemporarySeek(colNameOffset, SeekOrigin.Begin))
				Columns[i].Name = seek.Stream.ReadString(StringCoding.ZeroTerminated);
		}

		// Read Column types
		for (int i = 0; i < columnCount; i++)
			Columns[i].Type = (RRDBColumnType)bs.Read1Byte();

		bs.Position = rowDataStartOffset;

		// Read rows & cells
		for (int i = 0; i < rowCount; i++)
		{
			int basePos = rowDataStartOffset + rowLengthPadded * i;
			RRDBRowData row = new RRDBRowData();

			foreach (var col in Columns)
			{
				bs.Position = basePos + col.RowColumnOffset;
				IRRDBCell cell = null;
				if (col.Type == RRDBColumnType.String)
				{
					uint strPos = bs.ReadUInt32();
					string str;
					using (var seek = bs.TemporarySeek(strPos, SeekOrigin.Begin))
						str = seek.Stream.ReadString(StringCoding.ZeroTerminated);
					cell = new RRDBString(str);
				}
				else if (col.Type == RRDBColumnType.Integer)
				{
					cell = new RRDBInt(bs.ReadInt32());
				}
				else if (col.Type == RRDBColumnType.Float)
				{
					cell = new RRDBFloat(bs.ReadSingle());
				}
				else if (col.Type == RRDBColumnType.Byte)
				{
					cell = new RRDBByte(bs.Read1Byte());
				}
				else if (col.Type == RRDBColumnType.Short)
				{
					cell = new RRDBShort(bs.ReadInt16());
				}
				else
				{
					throw new Exception($"Type {col.Type}, row offset {col.RowColumnOffset.ToString("X8")}");
				}
				row.Cells.Add(cell);
			}

			Rows.Add(row);
		}
	}

	public void Save(string outputDir, bool bigEndian)
	{
		OptimizedStringTable strTable = new OptimizedStringTable();
		for (int i = 0; i < Columns.Count; i++)
			strTable.AddString(Columns[i].Name);

		for (int i = 0; i < Rows.Count; i++)
		{
			var row = Rows[i];
			for (int j = 0; j < row.Cells.Count; j++)
			{
				if (row.Cells[j] is RRDBString str)
					strTable.AddString(str.Value);
			}
		}

		// 1/2. Calculate where the string table is and save it
		using var fs = new FileStream(Path.Combine(outputDir, Name), FileMode.Create);
		using var bs = new BinaryStream(fs, bigEndian ? ByteConverter.Big : ByteConverter.Little);

		int rowLength = GetDataLengthAligned();

		bs.WriteBoolean(bigEndian);

		var tableConst = TableConsts.Find(e => e.TableName == Name);
		if (tableConst is not null)
		{
			bs.WriteByte(tableConst.nMajorID);
			bs.WriteByte(tableConst.Unk);
		}
		else
		{
			bs.WriteByte(0);
			bs.WriteByte(0);
		}

		bs.WriteByte((byte)Columns.Count);
		bs.WriteInt32(Rows.Count);
		bs.Position += 4;
		bs.Position += 4;

		bs.Position += Columns.Count * 4; // Column Offsets
		bs.Position += Columns.Count * 4; // Column Name Offsets
		bs.Position += Columns.Count; // Data types
		bs.AlignWithValue(0x77, 0x20);

		int rowDataOffset = (int)bs.Position;
		bs.Position += Rows.Count * rowLength;

		int strDbOffset = (int)bs.Position;
		strTable.SaveStream(bs);

		// 2/2. Backtrack, actually write stuff
		bs.Position = 0x08;
		bs.WriteInt32(rowDataOffset);
		bs.WriteInt32(rowLength);

		int currentRowTypeOffset = 0;
		foreach (var col in Columns) // Save column offsets to row data
		{
			bs.WriteInt32(currentRowTypeOffset);
			currentRowTypeOffset += col.GetTypeSize();
		}

		foreach (var col in Columns) // Save column name offsets
		{
			int strOffset = strTable.GetStringOffset(col.Name);
			bs.WriteInt32(strOffset);
		}

		foreach (var col in Columns) // Save column types
			bs.WriteByte((byte)col.Type);
		bs.AlignWithValue(0x77, 0x20);

		// Write row data
		foreach (var row in Rows)
		{
			foreach (var cell in row.Cells)
			{
				if (cell is RRDBString str)
					str.StringOffset = strTable.GetStringOffset(str.Value);
				cell.Serialize(bs);
			}

			bs.AlignWithValue(0x77, 0x20);
		}
	}

	public void ToCSV(string outputPath)
	{
		using var sw = new StreamWriter(outputPath);
		sw.WriteLine(string.Join(',', Columns.Select(e => e.Name)));

		foreach (var row in Rows)
			sw.WriteLine(string.Join(',', row.Cells.Select(c => c.ToString())));
	}

	private int GetDataLengthAligned()
	{
		int length = 0;
		foreach (var col in Columns)
			length += col.GetTypeSize();

		const int dataAlignment = 0x20;
		length += (-length % dataAlignment + dataAlignment) % dataAlignment;

		return length;
	}
}
