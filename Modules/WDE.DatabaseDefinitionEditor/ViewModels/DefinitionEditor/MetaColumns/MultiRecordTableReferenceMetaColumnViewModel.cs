using System;
using System.Diagnostics;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class MultiRecordTableReferenceMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string tableName = "";
    [Notify] private string keyColumnName = "";
    
    public override string Export()
    {
        return "tableByKey:" + tableName + ";" + keyColumnName;
    }

    public MultiRecordTableReferenceMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public MultiRecordTableReferenceMetaColumnViewModel(ColumnViewModel parent, string description) : this(parent)
    {
        if (!description.StartsWith("tableByKey:"))
            throw new ArgumentException("Expected tableByKey:");
        var parts = description.Substring("tableByKey:".Length).Split(';');
        tableName = parts[0];
        keyColumnName = parts[1];
    }
}