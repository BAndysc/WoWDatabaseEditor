using System;
using System.Diagnostics;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class ExpressionMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string expression = "''";
    
    public override string Export()
    {
        return "expression:" + expression;
    }

    public ExpressionMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public ExpressionMetaColumnViewModel(ColumnViewModel parent, string description) : this(parent)
    {
        if (!description.StartsWith("expression:"))
            throw new ArgumentException("Expected expression:");
        expression = description.Substring("expression:".Length);
    }
}