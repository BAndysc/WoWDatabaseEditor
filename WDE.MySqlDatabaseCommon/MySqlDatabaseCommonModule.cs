using Prism.Ioc;
using WDE.Module;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon
{
    public class MySqlDatabaseCommonModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            containerProvider.Resolve<DatabaseFileLogService>();
        }
    }
}