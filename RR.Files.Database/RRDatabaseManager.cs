using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;

using System.IO;
using RR.Files.Database.Types;

namespace RR.Files.Database;

public class RRDatabaseManager
{
    public List<Table> Tables = [];

    public static RRDatabaseManager FromDirectory(string directory)
    {
        var db = new RRDatabaseManager();

        foreach (var file in Directory.GetFiles(directory))
        {
            if (file.EndsWith(".csv") || file.EndsWith(".sqlite"))
                continue;

            var table = new Table(file);
            table.Read();
            db.Tables.Add(table);
        }

        return db;
    }

    public static RRDatabaseManager FromSQLite(string file)
    {
        var db = new RRDatabaseManager();
        using (var m_dbConnection = new SQLiteConnection($"Data Source={file};Version=3;"))
        {
            m_dbConnection.Open();

            ParseSQLiteTables(db, m_dbConnection);
        }

        return db;
    }

    private static void ParseSQLiteTables(RRDatabaseManager db, SQLiteConnection conn)
    {
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT name FROM sqlite_master";

        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var name = reader.GetString(0);
                if (name.EndsWith("_typeinfo"))
                    continue;
                else
                {
                    var table = new Table();
                    table.Name = name;

                    ParseTable(db, conn, table);
                    db.Tables.Add(table);
                }
            }
        }
    }

    private static void ParseTable(RRDatabaseManager db, SQLiteConnection conn, Table table)
    {
        // Parse column names
        var cmd = conn.CreateCommand();
        cmd.CommandText = @$"PRAGMA table_info({table.Name}_typeinfo);";
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var colName = reader.GetString(1);

                var rrColumn = new RRDBColumnInfo();
                rrColumn.Name = colName;
                table.Columns.Add(rrColumn);
            }
        }

        // Parse column types
        cmd.CommandText = $"SELECT * from {table.Name}_typeinfo;";
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string colType = reader.GetString(i);
                    if (!Enum.TryParse(colType, out RRDBColumnType res))
                        throw new Exception($"Unable to parse column type {colType}.");

                    table.Columns[i].Type = res;
                }
            }
        }

        // Parse row data
        cmd.CommandText = $"SELECT * FROM {table.Name};";
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                RRDBRowData row = new RRDBRowData();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    IRRDBCell cell;
                    if (table.Columns[i].Type == RRDBColumnType.Byte)
                        cell = new RRDBByte(reader.GetByte(i));
                    else if (table.Columns[i].Type == RRDBColumnType.Float)
                        cell = new RRDBFloat(reader.GetFloat(i));
                    else if (table.Columns[i].Type == RRDBColumnType.Integer)
                        cell = new RRDBInt(reader.GetInt32(i));
                    else if (table.Columns[i].Type == RRDBColumnType.Short)
                        cell = new RRDBShort(reader.GetInt16(i));
                    else if (table.Columns[i].Type == RRDBColumnType.String)
                        cell = new RRDBString(reader.GetString(i));
                    else
                    {
                        throw new Exception($"Could not parse from SQLite db: Invalid type {table.Columns[i].Type} in table {table.Name}");
                    }

                    row.Cells.Add(cell);

                }

                table.Rows.Add(row);
            }
        }
    }

    public void Save(string outputDir, bool bigEndian)
    {
        foreach (var table in Tables)
            table.Save(outputDir, bigEndian);
    }

    public void ExportAllToCSV(string outputDir)
    {
        foreach (var table in Tables)
        {
            table.ToCSV(Path.Combine(outputDir, table.Name + ".csv"));
        }
        Console.WriteLine("All tables successfully exported to CSV.");
    }
}
