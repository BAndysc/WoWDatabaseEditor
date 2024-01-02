using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common.Documents;
using WDE.Common.Managers;
using WDE.Common.Windows;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.ViewModels;

[AutoRegister]
[SingleInstance]
public partial class TablesToolViewModel : ObservableBase, ITool
{
    [Notify] private bool visibility;
    [Notify] private bool isSelected;
    public string Title => "Tables";
    public string UniqueId => "tables";
    public ToolPreferedPosition PreferedPosition => ToolPreferedPosition.Left;
    public bool OpenOnStart => true;
    private ITablesToolGroup? selectedGroup;

    public ObservableCollection<ITablesToolGroup> AllGroups { get; } = new();

    public TablesToolViewModel(IEnumerable<ITablesToolGroup> providers,
        IEnumerable<ITablesToolGroupsProvider> dynamicProviders)
    {
        foreach (var provider in providers
                     .Concat(dynamicProviders.SelectMany(x => x.GetProviders()))
                     .OrderByDescending(x => x.Priority))
            AllGroups.Add(provider);
        
        SelectedGroup = AllGroups.FirstOrDefault();
    }
    
    public ITablesToolGroup? SelectedGroup
    {
        get => selectedGroup;
        set
        {
            selectedGroup?.ToolClosed();
            SetProperty(ref selectedGroup, value);
            value?.ToolOpened();
        }
    }
}