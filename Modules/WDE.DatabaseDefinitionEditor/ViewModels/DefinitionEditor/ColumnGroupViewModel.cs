using DynamicData.Binding;
using PropertyChanged.SourceGenerator;
using WDE.DatabaseEditors.Data.Structs;
using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public partial class ColumnGroupViewModel : ObservableBase
{
    public DefinitionViewModel Parent { get; }
    [Notify] private string groupName;
    [Notify] private bool isExpanded;
    [Notify] private bool hasShowIf;
    [Notify] private string showIfColumnName = "";
    [Notify] private int showIfColumnValue;

    public ObservableCollectionExtended<ColumnViewModel> Columns { get; } = new();
    
    public ColumnGroupViewModel(DefinitionViewModel parent, DatabaseColumnsGroupJson group)
    {
        Parent = parent;
        groupName = group.Name;
        hasShowIf = group.ShowIf.HasValue;
        if (group.ShowIf.HasValue)
        {
            showIfColumnName = group.ShowIf.Value.ColumnName.ColumnName;
            showIfColumnValue = group.ShowIf.Value.Value;
        }
    }
}