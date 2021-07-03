using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Windows;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.MySqlDatabaseCommon.Database;
using WDE.MySqlDatabaseCommon.Database.Auth;
using WDE.MySqlDatabaseCommon.Database.World;
using WDE.MySqlDatabaseCommon.Services;
using WDE.MySqlDatabaseCommon.Tools;
using WDE.SkyFireMySqlDatabase.Database;

[assembly:ModuleRequiresCore("SkyFire")]
[assembly:ModuleBlocksOtherAttribute("WDE.TrinityMySqlDatabase")]

namespace WDE.SkyFireMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class SkyFireMySqlDatabaseModule : ModuleBase
    {
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            containerRegistry.RegisterSingleton<CachedDatabaseProvider>();
            containerRegistry.RegisterSingleton<NullWorldDatabaseProvider>();
            containerRegistry.RegisterSingleton<NullAuthDatabaseProvider>();
            containerRegistry.RegisterSingleton<SkyFireMySqlDatabaseProvider>();
            containerRegistry.RegisterSingleton<IMySqlExecutor, MySqlExecutor>();
            containerRegistry.RegisterSingleton<ICreatureStatCalculatorService, CreatureStatCalculatorService>();
            containerRegistry.RegisterSingleton<ICodeEditorViewModel, DebugQueryToolViewModel>();
        }
    }
}