using System;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Module;
using WDE.Module.Attributes;
using WDE.Updater.Services;
using WDE.Updater.ViewModels;

namespace WDE.Updater
{
    [AutoRegister]
    public class UpdaterModule : ModuleBase
    {
        public override void OnInitialized(IContainerProvider containerProvider)
        {
            containerProvider.Resolve<IEventAggregator>()
                .GetEvent<AllModulesLoaded>()
                .Subscribe(() => { containerProvider.Resolve<UpdateViewModel>(); }, ThreadOption.PublisherThread, true);
        }
    }
}