using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Services;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.SqlQueryGenerator;

[assembly:ModuleBlocksOther("WDE.TrinityMySqlDatabase")]
namespace WDE.HttpDatabase;

public class HttpDatabaseModule : ModuleBase
{
    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
        containerRegistry.RegisterSingleton<CachedDatabaseProvider>();
        containerRegistry.RegisterSingleton<NullWorldDatabaseProvider>();
        containerRegistry.RegisterSingleton<NullAuthDatabaseProvider>();
        containerRegistry.RegisterSingleton<HttpDatabaseProviderImpl>();
        containerRegistry.RegisterSingleton<IMySqlExecutor, DummyMySqlExecutor>();
        containerRegistry.RegisterSingleton<IMySqlHotfixExecutor, DummyHotfixMySqlExecutor>();
        containerRegistry.RegisterSingleton<IAuthMySqlExecutor, DummyAuthMySqlExecutor>();
        containerRegistry.RegisterSingleton<ICreatureStatCalculatorService, CreatureStatCalculatorService>();
        containerRegistry.RegisterSingleton<ICodeEditorViewModel, DebugQueryToolViewModel>();
    }
}

public class DummyMySqlExecutor : IMySqlExecutor
{
    public bool IsConnected { get; set; }
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

public class DummyHotfixMySqlExecutor : IMySqlHotfixExecutor
{
    public bool IsConnected { get; set; }
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