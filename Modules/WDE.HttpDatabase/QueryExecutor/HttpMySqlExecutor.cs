using System.Collections;
using System.Net.WebSockets;
using WDE.Common;
using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.HttpDatabase;

public class HttpMySqlExecutor : IMySqlExecutor
{
    private readonly HttpDatabaseProviderImpl impl;

    public HttpMySqlExecutor(HttpDatabaseProviderImpl impl)
    {
        this.impl = impl;
    }

    public bool IsConnected => true;

    public async Task ExecuteSql(IQuery query, bool rollback = false)
    {

    }

    public async Task ExecuteSql(string query, bool rollback = false)
    {
    }

    private object GetValueFromType(MySqlType type, string? value)
    {
        if (value == null)
            return DBNull.Value;

        switch (type)
        {
            case MySqlType.String:
                return value;
            case MySqlType.Int:
                return int.Parse(value);
            case MySqlType.Float:
                return float.Parse(value);
            case MySqlType.Double:
                return double.Parse(value);
            case MySqlType.Decimal:
                return decimal.Parse(value);
            case MySqlType.DateTime:
                return DateTime.Parse(value);
            case MySqlType.Bool:
                return bool.Parse(value);
            case MySqlType.UInt:
                return uint.Parse(value);
            case MySqlType.Long:
                return long.Parse(value);
            case MySqlType.ULong:
                return ulong.Parse(value);
            case MySqlType.Short:
                return short.Parse(value);
            case MySqlType.UShort:
                return ushort.Parse(value);
            case MySqlType.Byte:
                return byte.Parse(value);
            case MySqlType.SByte:
                return sbyte.Parse(value);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private System.Type GetTypeFromEnum(MySqlType type)
    {
        switch (type)
        {
            case MySqlType.String:
                return typeof(string);
            case MySqlType.Int:
                return typeof(int);
            case MySqlType.Float:
                return typeof(float);
            case MySqlType.Double:
                return typeof(double);
            case MySqlType.Decimal:
                return typeof(decimal);
            case MySqlType.DateTime:
                return typeof(DateTime);
            case MySqlType.Bool:
                return typeof(bool);
            case MySqlType.UInt:
                return typeof(uint);
            case MySqlType.Long:
                return typeof(long);
            case MySqlType.ULong:
                return typeof(ulong);
            case MySqlType.Short:
                return typeof(short);
            case MySqlType.UShort:
                return typeof(ushort);
            case MySqlType.Byte:
                return typeof(byte);
            case MySqlType.SByte:
                return typeof(sbyte);
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    public async Task<IDatabaseSelectResult> ExecuteSelectSql(string query)
    {
        try
        {
            var result = await impl.ExecuteAnyQuery(query);

            List<Dictionary<string, (Type, object)>> rows = new();
            foreach (var row in result.Rows)
            {
                Dictionary<string, (Type, object)> entity = new();
                for (int i = 0; i < row.Count; ++i)
                {
                    entity[result.Columns[i]] = (GetTypeFromEnum(result.Types[i]), GetValueFromType(result.Types[i], row[i]?.ToString()));
                }
                rows.Add(entity);
            }

            return new DatabaseSelectResults(rows);
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new EmptyDatabaseSelectResult();
        }
    }

    public async Task<IList<string>> GetTables()
    {
        return new List<string>();
    }

    public async Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table)
    {
        return new List<MySqlDatabaseColumn>();
    }

    private class DatabaseSelectResults : IDatabaseSelectResult
    {
        private List<Dictionary<string, (Type, object)>> data;
        private List<string> columns;
        private List<Type> types;

        public DatabaseSelectResults(List<Dictionary<string, (Type, object)>> data)
        {
            this.data = data;

            if (data.Count > 0)
            {
                columns = data[0].Keys.ToList();
                types = data[0].Values.Select(v => v.Item1).ToList();
            }
            else
            {
                columns = new List<string>();
                types = new List<Type>();
            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            for (int i = 0; i < data.Count; ++i)
                yield return i;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Columns => columns.Count;

        public int Rows => data.Count;

        public string ColumnName(int index) => data[0].Keys.ElementAt(index);

        public Type ColumnType(int index) => types[index];

        public object? Value(int row, int column) => data[row][columns[column]].Item2;

        public T? Value<T>(int row, int column) => (T?)Value(row, column);

        public bool IsNull(int row, int column) => Value(row, column) == null;

        public int ColumnIndex(string columnName) => columns.IndexOf(columnName);
    }
}