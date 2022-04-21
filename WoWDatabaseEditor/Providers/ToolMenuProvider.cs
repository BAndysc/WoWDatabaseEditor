using System;
using System.Collections.Generic;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Common.QuickAccess;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WoWDatabaseEditorCore.Services.FindAnywhere;

namespace WoWDatabaseEditorCore.Providers;

[AutoRegister]
public class ToolMenuProvider : IMainMenuItem
{
    public string ItemName => "Tools";
    public List<IMenuItem> SubItems { get; }
    public MainMenuItemSortPriority SortPriority => MainMenuItemSortPriority.PriorityNormal;

    public ToolMenuProvider(IEnumerable<IToolMenuItem> toolItems,
        IQuickAccessViewModel quickAccessViewModel,
        Lazy<IWindowManager> windowManager,
        Func<IFindAnywhereDialogViewModel> findAnywhereDialog)
    {
        SubItems = new List<IMenuItem>();
        
        SubItems.Add(new ModuleMenuItem("Open quick access",
            new DelegateCommand(() => quickAccessViewModel.OpenSearch("")),
            new("Control+R")));
            
        SubItems.Add(new ModuleMenuItem("Open quick commands",
            new DelegateCommand(() => quickAccessViewModel.OpenSearch("/")),
            new("Control+Shift+R")));

        foreach (var tool in toolItems)
        {
            SubItems.Add(tool);
        }
        
        SubItems.Add(new ModuleManuSeparatorItem());
            
        SubItems.Add(new ModuleMenuItem("Find anywhere",
            new AsyncAutoCommand(() => windowManager.Value.ShowDialog(findAnywhereDialog())),
            new("Control+Shift+F")));
    }
}