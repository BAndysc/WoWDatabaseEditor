﻿using System.Runtime.CompilerServices;
using Prism.Ioc;
using WDE.Common.Windows;
using WDE.Module;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.ViewModels;

[assembly: InternalsVisibleTo("WDE.SqlWorkbench.Test")]

namespace WDE.SqlWorkbench;

public class SqlWorkbenchModule : ModuleBase
{
    public override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        base.RegisterTypes(containerRegistry);
        var tool = new ActionsOutputViewModel();
        containerRegistry.RegisterInstance<IActionsOutputService>(tool);
        containerRegistry.RegisterInstance<ITool>(tool);
    }
}