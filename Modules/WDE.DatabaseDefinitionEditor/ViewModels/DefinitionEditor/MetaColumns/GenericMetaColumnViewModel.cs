using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class GenericMetaColumnViewModel : BaseMetaColumnViewModel
{
    private readonly ColumnViewModel parent;
    [Notify] private string text;
    
    public GenericMetaColumnViewModel(ColumnViewModel parent, 
        string text) : base(parent)
    {
        this.parent = parent;
        this.text = text;
    }

    public override string Export()
    {
        return text;
    }
}