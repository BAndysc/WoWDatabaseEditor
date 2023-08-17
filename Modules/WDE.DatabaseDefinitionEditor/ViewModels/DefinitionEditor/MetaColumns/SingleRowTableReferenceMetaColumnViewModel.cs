using System;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class SingleRowTableReferenceMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string tableName = "";
    [Notify] private string customWhere = "";
    [Notify] private string? partialKeyColumn; // note: it supports more than one column, but it's not used anywhere yet
    
    public override string Export()
    {
        return "table:" + tableName + ";" + customWhere + (!string.IsNullOrEmpty(partialKeyColumn) ? ";" + partialKeyColumn : "");
    }

    public SingleRowTableReferenceMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public SingleRowTableReferenceMetaColumnViewModel(ColumnViewModel parent, string description) : this(parent)
    {
        if (!description.StartsWith("table:"))
            throw new ArgumentException("Expected table:");
        var parts = description.Substring("table:".Length).Split(';');
        tableName = parts[0];
        customWhere = parts[1];
        if (parts.Length > 2)
            partialKeyColumn = parts[2];
    }
}