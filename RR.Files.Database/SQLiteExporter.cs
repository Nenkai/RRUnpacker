using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;
using RR.Files.Database.Types;
using System.Runtime.CompilerServices;

namespace RR.Files.Database;

// Exporting to SQLite is a bit unsuited
// They seem to use duplicate column names, aswell as non standard column names like 'addSpdMax.z'
// Commented out in Program.cs for now
public class SQLiteExporter
{
    private RRDatabaseManager _db;

    public SQLiteExporter(RRDatabaseManager db)
    {
        _db = db;
    }

    public void ExportToSQLite(string outputFile)
    {
        SQLiteConnection.CreateFile(outputFile);

        using (var m_dbConnection = new SQLiteConnection($"Data Source={outputFile};Version=3;"))
        {
            m_dbConnection.Open();

            CreateTables(m_dbConnection);
            CreateTableInfo(m_dbConnection);
            InsertTableInfo(m_dbConnection);
            InsertTableRows(m_dbConnection);
        }
    }

    private void CreateTables(SQLiteConnection conn)
    {
        foreach (var table in _db.Tables)
        {
            Console.WriteLine($"SQLite: Creating table {table.Name}");

            StringBuilder sb = new StringBuilder();
            sb.Append($"CREATE TABLE {table.Name} (");

            Dictionary<string, int> _columnNames = [];
            for (int i = 0; i < table.Columns.Count; i++)
            {
                RRDBColumnInfo col = table.Columns[i];

                string name = col.Name;
                if (_columnNames.TryGetValue(name, out int value))
                {
                    if (value != 0)
                    {
                        Console.WriteLine($"SQLite: Warning - Column {name} already exists in table {table.Name}");
                        name = $"{name}_{value}";
                    }
                }
                else
                    _columnNames.Add(name, 0);

                _columnNames[col.Name]++;

                sb.Append(TranslateColumnNameForSQLite(name));
                sb.Append($" {TranslateColumnTypeToSQLiteType(col)}");

                if (i < table.Columns.Count - 1)
                    sb.Append(", ");


            }

            sb.Append(')');
            SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
            command.ExecuteNonQuery();
        }
    }

    private void CreateTableInfo(SQLiteConnection conn)
    {
        foreach (var table in _db.Tables)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"CREATE TABLE {table.Name}_typeinfo (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                RRDBColumnInfo col = table.Columns[i];
                sb.Append(TranslateColumnNameForSQLite(col.Name));
                sb.Append($" varchar(64)");

                if (i < table.Columns.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(')');
            SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
            command.ExecuteNonQuery();
        }
    }

    private void InsertTableInfo(SQLiteConnection conn)
    {
        foreach (var table in _db.Tables)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"INSERT INTO {table.Name}_typeinfo (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                RRDBColumnInfo col = table.Columns[i];

                sb.Append(TranslateColumnNameForSQLite(col.Name));
                if (i < table.Columns.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(") values (");
            for (int i = 0; i < table.Columns.Count; i++)
            {
                RRDBColumnInfo col = table.Columns[i];
                sb.Append($"'{col.Type}'");
                if (i < table.Columns.Count - 1)
                    sb.Append(", ");
            }
            sb.Append(')');
            SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
            command.ExecuteNonQuery();
        }
    }

    private void InsertTableRows(SQLiteConnection conn)
    {
        List<Table> list = _db.Tables.ToList();
        for (int i1 = 0; i1 < list.Count; i1++)
        {
            var table = list[i1];
            StringBuilder sb = new StringBuilder();
            sb.Append($"INSERT INTO {table.Name} (");

            for (int i = 0; i < table.Columns.Count; i++)
            {
                RRDBColumnInfo col = table.Columns[i];
                sb.Append(TranslateColumnNameForSQLite(col.Name));
                if (i < table.Columns.Count - 1)
                    sb.Append(", ");
            }

            sb.Append(") values (");

            string preInsertQuery = sb.ToString();

            using (var transac = conn.BeginTransaction())
            {
                for (int i = 0; i < table.Rows.Count; i++)
                {
                    sb.Clear();
                    var row = table.Rows[i];
                    for (int j = 0; j < row.Cells.Count; j++)
                    {
                        RRDBColumnInfo col = table.Columns[j];


                        switch (col.Type)
                        {
                            case RRDBColumnType.Byte:
                                sb.Append((row.Cells[j] as RRDBByte).Value); break;
                            case RRDBColumnType.Short:
                                sb.Append((row.Cells[j] as RRDBShort).Value); break;
                            case RRDBColumnType.Integer:
                                sb.Append((row.Cells[j] as RRDBInt).Value); break;
                            case RRDBColumnType.String:
                                sb.Append($"'{(row.Cells[j] as RRDBString).Value.Replace("'", "''")}'"); break;
                            case RRDBColumnType.Float:
                                sb.Append((row.Cells[j] as RRDBFloat).Value); break;
                        }

                        if (j < row.Cells.Count - 1)
                            sb.Append(", ");
                    }

                    sb.Append(')');
                    SQLiteCommand command = new SQLiteCommand(preInsertQuery + sb.ToString(), conn);
                    command.ExecuteNonQuery();
                }

                transac.Commit();
            }
        }
    }

    static string TranslateColumnNameForSQLite(string name)
    {
        if (int.TryParse(name, out _))
            return $"'{name}'";
        else
            return name;
    }

    static string TranslateColumnTypeToSQLiteType(RRDBColumnInfo col)
    {
        if (col.Type == RRDBColumnType.String)
            return "TEXT";
        else if (col.Type == RRDBColumnType.Float)
            return "REAL";
        else
            return "INT";
    }
}
