using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.HttpDatabase;

public class DummyAuthMySqlExecutor : IAuthMySqlExecutor
{
    public bool IsConnected { get; set; }
    public async Task ExecuteSql(string query)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteSql(IQuery query, bool rollback = false)
    {
        throw new NotImplementedException();
    }

    public async Task ExecuteSql(string query, bool rollback = false)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<Dictionary<string, (Type, object)>>> ExecuteSelectSql(string query)
    {
        throw new NotImplementedException();
    }

    public async Task<IList<string>> GetTables()
    {
        throw new NotImplementedException();
    }

    public async Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table)
    {
        throw new NotImplementedException();
    }
}