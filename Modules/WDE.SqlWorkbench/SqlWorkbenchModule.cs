using System.Runtime.CompilerServices;
using Prism.Ioc;
using WDE.Common.Tasks;
using WDE.Common.Windows;
using WDE.Module;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.ViewModels;

[assembly: InternalsVisibleTo("WDE.SqlWorkbench.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace WDE.SqlWorkbench;

public class SqlWorkbenchModule : ModuleBase
{
    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
        var tool = new ActionsOutputViewModel(((IContainerProvider)containerRegistry).Resolve<IMainThread>());
        containerRegistry.RegisterInstance<IActionsOutputService>(tool);
        containerRegistry.RegisterInstance<ITool>(tool, nameof(ActionsOutputViewModel));
    }
}