using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Services;
using WDE.MySqlDatabaseCommon.Tools;

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
        containerRegistry.RegisterSingleton<IMySqlExecutor, HttpMySqlExecutor>();
        containerRegistry.RegisterSingleton<IMySqlHotfixExecutor, DummyHotfixMySqlExecutor>();
        containerRegistry.RegisterSingleton<IAuthMySqlExecutor, DummyAuthMySqlExecutor>();
        containerRegistry.RegisterSingleton<ICreatureStatCalculatorService, CreatureStatCalculatorService>();
        containerRegistry.RegisterSingleton<ICodeEditorViewModel, DebugQueryToolViewModel>();
    }
}