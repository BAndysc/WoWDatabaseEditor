using Prism.Ioc;
using WDE.Common.Database;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Services;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.CMMySqlDatabase.Database;


[assembly: ModuleRequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
[assembly: ModuleBlocksOtherAttribute("WDE.TrinityMySqlDatabase")]

namespace WDE.CMMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class CMMySqlDatabaseModule : ModuleBase
    {
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            containerRegistry.RegisterSingleton<CachedDatabaseProvider>();
            containerRegistry.RegisterSingleton<NullWorldDatabaseProvider>();
            containerRegistry.RegisterSingleton<NullAuthDatabaseProvider>();
            containerRegistry.RegisterSingleton<IMySqlExecutor, WorldMySqlExecutor>();
            containerRegistry.RegisterSingleton<IMySqlHotfixExecutor, NullHotfixMysqlExecutor>();
            containerRegistry.RegisterSingleton<IAuthMySqlExecutor, AuthMySqlExecutor>();
            containerRegistry.RegisterSingleton<ICreatureStatCalculatorService, CreatureStatCalculatorService>();
            containerRegistry.RegisterSingleton<ICodeEditorViewModel, DebugQueryToolViewModel>();
        }
    }
}