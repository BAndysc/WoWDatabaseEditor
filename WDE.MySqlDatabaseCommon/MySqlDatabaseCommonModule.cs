using Prism.Ioc;
using WDE.Module;
using WDE.MySqlDatabaseCommon.Services;

namespace WDE.MySqlDatabaseCommon
{
    public class MySqlDatabaseCommonModule : ModuleBase
    {
        private IContainerProvider? provider;

        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            provider = containerProvider;
        }

        public override void FinalizeRegistration(IContainerRegistry container)
        {
            base.FinalizeRegistration(container);
            provider?.Resolve<DatabaseFileLogService>();
        }
    }
}