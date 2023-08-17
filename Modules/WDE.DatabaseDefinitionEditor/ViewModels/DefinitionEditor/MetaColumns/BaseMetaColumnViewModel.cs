using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public abstract partial class BaseMetaColumnViewModel : ObservableBase
{
    public ColumnViewModel Parent { get; }

    public BaseMetaColumnViewModel(ColumnViewModel parent)
    {
        Parent = parent;
    }
    
    public abstract string Export();
}