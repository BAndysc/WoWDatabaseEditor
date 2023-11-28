using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;

namespace WDE.SqlWorkbench.Services.Connection;

internal class MySqlSession : IMySqlSession, IAsyncDisposable
{
    private readonly MySqlConnection conn;

    public MySqlSession(MySqlConnection conn)
    {
        this.conn = conn;
    }

    public bool IsSessionOpened => conn.State == ConnectionState.Open;

    public async Task<bool> TryReconnectAsync()
    {
        await conn.OpenAsync();
        return IsSessionOpened;
    }

    public async Task<SelectResult> ExecuteSqlAsync(string query, int? rowsLimit, CancellationToken token)
    {
        return await Task.Run(async () =>
        {
            await using MySqlCommand cmd = new(query, conn);
            await using MySqlDataReader reader = await cmd.ExecuteReaderAsync(token);

            var affected = reader.RecordsAffected;

            var schema = await reader.GetColumnSchemaAsync(token);

            if (schema.Count > 0)
            {
                var columnNames = new string[schema.Count];
                var columnTypes = new Type?[schema.Count];
                var columnData = new IColumnData[schema.Count];

                for (int i = 0; i < schema.Count; ++i)
                {
                    columnNames[i] = schema[i].ColumnName;
                    columnTypes[i] = schema[i].DataType;
                    var dataType = schema[i].DataType;
                    if (dataType == typeof(string))
                        columnData[i] = new StringColumnData();
                    else if (dataType == typeof(bool))
                        columnData[i] = new BooleanColumnData();
                    else if (dataType == typeof(byte))
                        columnData[i] = new ByteColumnData();
                    else if (dataType == typeof(sbyte))
                        columnData[i] = new SByteColumnData();
                    else if (dataType == typeof(short))
                        columnData[i] = new Int16ColumnData();
                    else if (dataType == typeof(ushort))
                        columnData[i] = new UInt16ColumnData();
                    else if (dataType == typeof(int))
                        columnData[i] = new Int32ColumnData();
                    else if (dataType == typeof(uint))
                        columnData[i] = new UInt32ColumnData();
                    else if (dataType == typeof(long))
                        columnData[i] = new Int64ColumnData();
                    else if (dataType == typeof(ulong))
                        columnData[i] = new UInt64ColumnData();
                    else if (dataType == typeof(char))
                        columnData[i] = new CharColumnData();
                    else if (dataType == typeof(decimal))
                        columnData[i] = new DecimalColumnData();
                    else if (dataType == typeof(double))
                        columnData[i] = new DoubleColumnData();
                    else if (dataType == typeof(float))
                        columnData[i] = new FloatColumnData();
                    else if (dataType == typeof(DateTime))
                        columnData[i] = new DateTimeColumnData();
                    else if (dataType == typeof(DateTimeOffset))
                        columnData[i] = new DateTimeOffsetColumnData();
                    else if (dataType == typeof(Guid))
                        columnData[i] = new GuidColumnData();
                    else if (dataType == typeof(TimeSpan))
                        columnData[i] = new TimeSpanColumnData();
                    else if (dataType == typeof(byte[]))
                        columnData[i] = new BinaryColumnData();
                    else
                        columnData[i] = new ObjectColumnData();
                }

                int rows = 0;
                while (await reader.ReadAsync(token) && (rowsLimit == null || rows < rowsLimit))
                {
                    for (int fieldIndex = 0; fieldIndex < reader.FieldCount; ++fieldIndex)
                    {
                        columnData[fieldIndex].Append(reader, fieldIndex);
                    }

                    if (token.IsCancellationRequested)
                        break;
                    rows++;
                }

                return SelectResult.Query(columnNames, columnTypes, rows, columnData);
            }
            else
            {
                return SelectResult.NonQuery(affected);
            }
        }, token);
    }

    public async Task<IReadOnlyList<string>> GetDatabasesAsync(CancellationToken token = default)
    {
        var databases = await ExecuteSqlAsync("SHOW DATABASES", null, token);

        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return Array.Empty<string>();

        return Enumerable.Range(0, databases.AffectedRows)
            .Select(x => databases.Columns[0]!.GetToString(x)!)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetTablesAsync(CancellationToken token)
    {
        var databases = await ExecuteSqlAsync("SHOW TABLES", null, token);

        if (databases.IsNonQuery || databases.Columns.Length == 0)
            return Array.Empty<string>();

        return Enumerable.Range(0, databases.AffectedRows)
            .Select(x => databases.Columns[0]!.GetToString(x)!)
            .ToList();
    }

    public async Task<IReadOnlyList<ColumnInfo>> GetTableColumnsAsync(string tableName, CancellationToken token)
    {
        List<ColumnInfo> columns = new();
        if (!tableName.StartsWith('`'))
            tableName = $"`{tableName}`";
        
        var databases = await ExecuteSqlAsync($"SHOW COLUMNS FROM {tableName}", null, token);
        if (token.IsCancellationRequested)
            return columns;

        var names = (StringColumnData)databases["Field"]!;
        var types = (StringColumnData)databases["Type"]!;
        var nullables = (StringColumnData)databases["Null"]!;
        var keys = (StringColumnData)databases["Key"]!;
        var defaults = (StringColumnData)databases["Default"]!;
        var extras = (StringColumnData)databases["Extra"]!;
        for (int i = 0; i < databases.AffectedRows; ++i)
        {
            var column = new ColumnInfo(
                names[i]!,
                types[i]!,
                nullables[i] == "YES",
                keys[i] == "PRI",
                extras[i]!.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                defaults[i]);
            columns.Add(column);
        }

        return columns;
    }

    public async Task<string> GetCreateTableAsync(string tableName, CancellationToken token = default)
    {
        var databases = await ExecuteSqlAsync($"SHOW CREATE TABLE `{tableName}`", null, token);
        if (token.IsCancellationRequested)
            return "";

        var createTable = (StringColumnData)databases["Create Table"]!;
        return createTable[0]!;
    }

    public async ValueTask DisposeAsync()
    {
        await conn.DisposeAsync();
    }

    public void Dispose()
    {
        conn.Dispose();
    }
}