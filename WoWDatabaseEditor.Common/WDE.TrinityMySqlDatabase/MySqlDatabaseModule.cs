using Prism.Ioc;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Database;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class TrinityMySqlDatabaseModule : ModuleBase
    {
        public override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            base.RegisterTypes(containerRegistry);
            containerRegistry.RegisterSingleton<CachedDatabaseProvider>();
            containerRegistry.RegisterSingleton<TrinityMySqlDatabaseProvider>();
            containerRegistry.RegisterSingleton<NullDatabaseProvider>();
        }
    }
}