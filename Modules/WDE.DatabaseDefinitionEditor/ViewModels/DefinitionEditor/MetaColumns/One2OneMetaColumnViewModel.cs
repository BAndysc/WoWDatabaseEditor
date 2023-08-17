using System;
using PropertyChanged.SourceGenerator;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

public partial class One2OneMetaColumnViewModel : BaseMetaColumnViewModel
{
    [Notify] private string otherTable = "";
    [Notify] private bool dynamicKey;

    public One2OneMetaColumnViewModel(ColumnViewModel parent) : base(parent)
    {
    }

    public One2OneMetaColumnViewModel(ColumnViewModel parent, string description) : this(parent)
    {
        if (description.StartsWith("one2one:"))
            otherTable = description.Substring("one2one:".Length);
        else if (description.StartsWith("one2one_dynamic_key:"))
        {
            otherTable = description.Substring("one2one_dynamic_key:".Length);
            dynamicKey = true;
        }
        else
            throw new Exception("Invalid description for one2one meta column: " + description);
    }

    public override string Export()
    {
        if (dynamicKey)
            return "one2one_dynamic_key:" + otherTable;
        else
           return "one2one:" + otherTable;
    }
}