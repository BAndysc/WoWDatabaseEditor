using WDE.MVVM;

namespace WDE.DatabaseDefinitionEditor.ViewModels.DefinitionEditor;

public class DatabaseSourceColumnViewModel : ObservableBase
{
    public DatabaseSourceColumnViewModel(string columnName, string tableName, bool isForeignTable, string typeName, bool nullable)
    {
        ColumnName = columnName;
        TableName = tableName;
        IsForeignTable = isForeignTable;
        TypeName = typeName;
        Nullable = nullable;
    }

    public string ColumnName { get; }
    public string TableName { get; }
    public bool IsForeignTable { get; }
    public string TypeName { get; }
    public bool Nullable { get; }

    public override string ToString()
    {
        return ColumnName;
    }
}