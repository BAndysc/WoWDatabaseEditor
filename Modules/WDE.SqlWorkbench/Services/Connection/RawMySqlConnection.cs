using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.Connection;

internal class RawMySqlConnection : IRawMySqlConnection, IAsyncDisposable
{
    private readonly MySqlConnection conn;
    private readonly IQuerySafetyService querySafetyService;
    private readonly QueryExecutionSafety safety;

    public RawMySqlConnection(MySqlConnection conn, IQuerySafetyService querySafetyService, QueryExecutionSafety safety)
    {
        this.conn = conn;
        this.querySafetyService = querySafetyService;
        this.safety = safety;
    }

    public bool IsSessionOpened => conn.State == ConnectionState.Open;

    public async Task<bool> TryReconnectAsync()
    {
        await conn.OpenAsync();
        return IsSessionOpened;
    }

    public async Task<SelectResult> ExecuteSqlAsync(string query, int? rowsLimit, CancellationToken token)
    {
        if (!await querySafetyService.CanExecuteAsync(query, safety))
        {
            throw new TaskCanceledException("The user canceled the query execution");
        }
        
        if (!IsSessionOpened)
            await TryReconnectAsync();

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
                    columnData[i] = IColumnData.CreateTypedColumn(dataType, schema[i].DataTypeName);
                }

                int rows = 0;
                var wrappedReader = new MySqlDataReaderWrapper(reader);
                while (await reader.ReadAsync(token) && (rowsLimit == null || rows < rowsLimit))
                {
                    for (int fieldIndex = 0; fieldIndex < reader.FieldCount; ++fieldIndex)
                    {
                        columnData[fieldIndex].Append(wrappedReader, fieldIndex);
                    }

                    if (token.IsCancellationRequested)
                        break;
                    rows++;
                }

                return SelectResult.Query(columnNames, columnTypes, rows, columnData);
            }

            return SelectResult.NonQuery(affected);
        }, token);
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

internal class MySqlDataReaderWrapper : IMySqlDataReader
{
    private MySqlDataReader reader;

    public MySqlDataReaderWrapper(MySqlDataReader reader)
    {
        this.reader = reader;
    }

    public bool IsDBNull(int ordinal)
    {
        return reader.IsDBNull(ordinal);
    }

    public object? GetValue(int ordinal)
    {
        return reader.GetValue(ordinal);
    }

    public string? GetString(int ordinal)
    {
        return reader.GetString(ordinal);
    }

    public bool GetBoolean(int ordinal)
    {
        return reader.GetBoolean(ordinal);
    }

    public byte GetByte(int ordinal)
    {
        return reader.GetByte(ordinal);
    }

    public sbyte GetSByte(int ordinal)
    {
        return reader.GetSByte(ordinal);
    }

    public short GetInt16(int ordinal)
    {
        return reader.GetInt16(ordinal);
    }

    public ushort GetUInt16(int ordinal)
    {
        return reader.GetUInt16(ordinal);
    }

    public int GetInt32(int ordinal)
    {
        return reader.GetInt32(ordinal);
    }

    public uint GetUInt32(int ordinal)
    {
        return reader.GetUInt32(ordinal);
    }

    public long GetInt64(int ordinal)
    {
        return reader.GetInt64(ordinal);
    }

    public ulong GetUInt64(int ordinal)
    {
        return reader.GetUInt64(ordinal);
    }

    public char GetChar(int ordinal)
    {
        return reader.GetChar(ordinal);
    }

    public PublicMySqlDecimal GetDecimal(int ordinal)
    {
        return PublicMySqlDecimal.FromDecimal(reader.GetMySqlDecimal(ordinal));
    }

    public double GetDouble(int ordinal)
    {
        return reader.GetDouble(ordinal);
    }

    public float GetFloat(int ordinal)
    {
        return reader.GetFloat(ordinal);
    }

    public DateTime GetDateTime(int ordinal)
    {
        return reader.GetDateTime(ordinal);
    }

    public DateTimeOffset GetDateTimeOffset(int ordinal)
    {
        return reader.GetDateTimeOffset(ordinal);
    }

    public Guid GetGuid(int ordinal)
    {
        return reader.GetGuid(ordinal);
    }

    public TimeSpan GetTimeSpan(int ordinal)
    {
        return reader.GetTimeSpan(ordinal);
    }

    public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        return reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    }

    public MySqlDateTime GetMySqlDateTime(int ordinal)
    {
        return reader.GetMySqlDateTime(ordinal);
    }
}