using System;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class CustomFieldMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string parameterName = "''";
    
    public override string Export()
    {
        return "customfield:" + parameterName;
    }

    public CustomFieldMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public CustomFieldMetaColumnViewModel(ColumnViewModel parent, string description) : this(parent)
    {
        if (!description.StartsWith("customfield:"))
            throw new ArgumentException("Expected customfield:");
        parameterName = description.Substring("customfield:".Length);
    }
}