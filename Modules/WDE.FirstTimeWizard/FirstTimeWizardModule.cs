using System;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.CoreVersion;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.FirstTimeWizard.Services;
using WDE.Module;

namespace WDE.FirstTimeWizard;

public class FirstTimeWizardModule : ModuleBase
{
    public FirstTimeWizardModule(IContainerProvider containerProvider)
    {
        containerProvider.Resolve<IEventAggregator>().GetEvent<AllModulesLoaded>()
            .Subscribe(() =>
            {
                DispatcherTimer.RunOnce(() =>
                {
                    containerProvider.Resolve<IFirstTimeWizardService>().Run();
                }, TimeSpan.FromMilliseconds(1));
            }, true);
    }
}