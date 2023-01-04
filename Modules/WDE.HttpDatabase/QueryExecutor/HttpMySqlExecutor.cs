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

    public async Task<IList<Dictionary<string, (Type, object)>>> ExecuteSelectSql(string query)
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

            return rows;
        }
        catch (Exception e)
        {
            LOG.LogError(e);
            return new List<Dictionary<string, (Type, object)>>();
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
}