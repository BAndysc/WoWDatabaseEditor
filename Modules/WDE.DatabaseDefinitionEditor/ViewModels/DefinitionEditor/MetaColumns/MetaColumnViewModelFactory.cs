using System;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor.MetaColumns;

[UniqueProvider]
public interface IMetaColumnViewModelFactory
{
    MetaColumnTypeViewModel[] Types { get; }
    BaseMetaColumnViewModel Factory(ColumnViewModel parent, string description);
}

[AutoRegister]
[SingleInstance]
public class MetaColumnViewModelFactory : IMetaColumnViewModelFactory
{
    public MetaColumnTypeViewModel[] Types { get; } = new[]
    {
        new MetaColumnTypeViewModel("table", 
            "Reference to a Single Row table",
            "A button which opens a window with the referenced single row table.",
            p => new SingleRowTableReferenceMetaColumnViewModel(p)),
        
        new MetaColumnTypeViewModel("tableByKey",
            "Reference to a Multi Record table",
            "A button which opens a window with the referenced single row table (one to many relation).",
            p => new MultiRecordTableReferenceMetaColumnViewModel(p)),
        
        new MetaColumnTypeViewModel("one2one",
            "One to one relation", 
            "A button which opens a special window with only single row displayed in a vertical form.", 
            p => new One2OneMetaColumnViewModel(p)),
        
        new MetaColumnTypeViewModel("expression",
            "Expression", 
            "An evaluated expression using JavaScript like syntax.", 
            p => new ExpressionMetaColumnViewModel(p)),

        new MetaColumnTypeViewModel("customfield",
            "Custom field",
            "A custom field which is not stored in the database, it is up the parameter implementation to decide where the data is stored. \nUnlike expression, this is read-write (expression is read only) and the implementation belongs to the C# code.",
            p => new CustomFieldMetaColumnViewModel(p)),
        
        new MetaColumnTypeViewModel("invoke",
            "Invoke command",
            "A button which invokes a remote command via SOAP or another chosen remote connection method.",
            p => new InvokeRemoteCommandMetaColumnViewModel(p)),
        
        new MetaColumnTypeViewModel("command",
            "Editor command",
            "A button which executes a chosen editor command for the table",
            p => new TableCommandMetaColumnViewModel(p)),
    };

    public BaseMetaColumnViewModel Factory(ColumnViewModel parent, string description)
    {
        // note: this is not a good design. The meta column parsing should belong to the WDE.DatabaseEditor itself
        // maybe one day I shall rewrite it in this manner
        if (description.StartsWith("expression:"))
            return new ExpressionMetaColumnViewModel(parent, description);
        if (description.StartsWith("customfield:"))
            return new CustomFieldMetaColumnViewModel(parent, description);
        if (description.StartsWith("invoke:"))
            return new InvokeRemoteCommandMetaColumnViewModel(parent, description);
        if (description.StartsWith("command:"))
            return new TableCommandMetaColumnViewModel(parent, description);
        if (description.StartsWith("tableByKey:"))
            return new MultiRecordTableReferenceMetaColumnViewModel(parent, description);
        if (description.StartsWith("one2one_dynamic_key:") || description.StartsWith("one2one:"))
            return new One2OneMetaColumnViewModel(parent, description);
        if (description.StartsWith("table:"))
            return new SingleRowTableReferenceMetaColumnViewModel(parent, description);
        return new GenericMetaColumnViewModel(parent, description);
    }
}

public class MetaColumnTypeViewModel
{
    public MetaColumnTypeViewModel(string prefix, string name, string description, Func<ColumnViewModel, BaseMetaColumnViewModel> factory)
    {
        Prefix = prefix;
        Name = name;
        Description = description;
        Factory = factory;
    }

    public string Prefix { get; }
    public string Name { get; }
    public string Description { get; }
    public Func<ColumnViewModel, BaseMetaColumnViewModel> Factory { get; }
}