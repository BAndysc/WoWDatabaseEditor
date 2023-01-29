using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.SqlQueryGenerator;

namespace WDE.MySqlDatabaseCommon.Database;

public class NullHotfixMysqlExecutor : IMySqlHotfixExecutor
{
    public bool IsConnected => false;
    
    public async Task ExecuteSql(IQuery query, bool rollback = false)
    {
    }

    public async Task ExecuteSql(string query, bool rollback = false)
    {
    }

    public async Task<IList<Dictionary<string, (Type, object)>>> ExecuteSelectSql(string query)
    {
        return new List<Dictionary<string, (Type, object)>>();
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