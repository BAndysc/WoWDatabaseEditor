using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services;

[UniqueProvider]
public interface IDatabaseQueryExecutor
{
    Task<IDatabaseSelectResult> ExecuteSelectSql(DatabaseTableDefinitionJson definition, string query);
    Task ExecuteSql(DatabaseTableDefinitionJson definition, IQuery query, bool rollback = false);
    Task ExecuteSql(DatabaseTableDefinitionJson definition, string query, bool rollback = false);
    Task<IList<DatabaseTable>> GetTables(DataDatabaseType type);
    Task<IList<MySqlDatabaseColumn>> GetTableColumns(DataDatabaseType type, string table);
    bool IsConnected(DatabaseTableDefinitionJson tableDefinition);
}

[AutoRegister]
[SingleInstance]
internal class DatabaseQueryExecutor : IDatabaseQueryExecutor
{
    private readonly IMySqlExecutor worldExecutor;
    private readonly IMySqlHotfixExecutor hotfixExecutor;

    public DatabaseQueryExecutor(IMySqlExecutor worldExecutor,
        IMySqlHotfixExecutor hotfixExecutor)
    {
        this.worldExecutor = worldExecutor;
        this.hotfixExecutor = hotfixExecutor;
    }
    
    public Task<IDatabaseSelectResult> ExecuteSelectSql(DatabaseTableDefinitionJson definition, string query)
    {
        if (definition.DataDatabaseType == DataDatabaseType.World)
            return worldExecutor.ExecuteSelectSql(query);
        else
            return hotfixExecutor.ExecuteSelectSql(query);
    }

    public Task ExecuteSql(DatabaseTableDefinitionJson definition, IQuery query, bool rollback = false)
    {
        if (definition.DataDatabaseType == DataDatabaseType.World)
            return worldExecutor.ExecuteSql(query, rollback);
        else
            return hotfixExecutor.ExecuteSql(query, rollback);
    }

    public Task ExecuteSql(DatabaseTableDefinitionJson definition, string query, bool rollback = false)
    {
        if (definition.DataDatabaseType == DataDatabaseType.World)
            return worldExecutor.ExecuteSql(query, rollback);
        else
            return hotfixExecutor.ExecuteSql(query, rollback);
    }

    public async Task<IList<DatabaseTable>> GetTables(DataDatabaseType type)
    {
        IList<string> tables;
        if (type == DataDatabaseType.World)
            tables = await worldExecutor.GetTables();
        else
        {
            Debug.Assert(type == DataDatabaseType.Hotfix);
            tables = await hotfixExecutor.GetTables();
        }

        return tables.Select(x => new DatabaseTable(type, x)).ToList();
    }

    public Task<IList<MySqlDatabaseColumn>> GetTableColumns(DataDatabaseType type, string table)
    {
        if (type == DataDatabaseType.World)
            return worldExecutor.GetTableColumns(table);
        
        Debug.Assert(type == DataDatabaseType.Hotfix);
        return hotfixExecutor.GetTableColumns(table);
    }

    public bool IsConnected(DatabaseTableDefinitionJson tableDefinition)
    {
        if (tableDefinition.DataDatabaseType == DataDatabaseType.World)
            return worldExecutor.IsConnected;
        return hotfixExecutor.IsConnected;
    }
}