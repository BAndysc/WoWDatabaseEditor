using Prism.Ioc;
using WDE.Module;
using WoWDatabaseEditorCore.Services.Statistics;

namespace WoWDatabaseEditorCore
{
    public class MainModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            base.OnInitialized(containerProvider);
            containerProvider.Resolve<StatisticsService>();
        }
    }
}