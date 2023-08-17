using System;
using System.Diagnostics;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class TableCommandMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string commandId = "";
    [Notify] private string? customButtonText;
    
    public override string Export()
    {
        return "command:" + commandId + (customButtonText != null ? ":" + customButtonText : "");
    }

    public TableCommandMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public TableCommandMetaColumnViewModel(ColumnViewModel parent, string description) : base(parent)
    {
        if (!description.StartsWith("command:"))
            throw new ArgumentException("Expected command:");
        var parts = description.Substring("command:".Length).Split(':');
        commandId = parts[0];
        if (parts.Length > 1)
            customButtonText = parts[1];
    }
}