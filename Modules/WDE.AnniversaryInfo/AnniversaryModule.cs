using System;
using Avalonia.Threading;
using Prism.Events;
using Prism.Ioc;
using WDE.Common.Events;
using WDE.Common.Services;
using WDE.Module;
using WDE.Module.Attributes;

namespace WDE.AnniversaryInfo;

[AutoRegister]
public class AnniversaryModule : ModuleBase
{
    public override void OnInitialized(IContainerProvider containerProvider)
    {
        base.OnInitialized(containerProvider);
        containerProvider.Resolve<IEventAggregator>().GetEvent<AllModulesLoaded>()
            .Subscribe(() =>
            {
                DispatcherTimer.RunOnce(() =>
                {
                    containerProvider.Resolve<IAnniversarySummaryService>().TryOpenDefaultSummary();
                }, TimeSpan.FromMilliseconds(1));
            }, true);
    }
}