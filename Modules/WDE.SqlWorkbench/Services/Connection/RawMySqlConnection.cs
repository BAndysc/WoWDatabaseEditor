using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using MySqlConnector;
using WDE.Common.Services.MessageBox;
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
                    else if (dataType == typeof(MySqlDateTime))
                        columnData[i] = new MySqlDateTimeColumnData();
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