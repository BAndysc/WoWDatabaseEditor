using System;
using System.Diagnostics;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class InvokeRemoteCommandMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string command = "";
    
    public override string Export()
    {
        return "invoke:" + command;
    }

    public InvokeRemoteCommandMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public InvokeRemoteCommandMetaColumnViewModel(ColumnViewModel parent, string description) : base(parent)
    {
        if (!description.StartsWith("invoke:"))
            throw new ArgumentException("Expected invoke:");
        command = description.Substring("invoke:".Length);
    }
}