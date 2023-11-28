using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class TableViewModel : ObservableBase, IChildType
{
    public TableViewModel(SchemaViewModel parent, string schemaName, string tableName)
    {
        TableName = tableName;
        Parent = parent;
        Schema = parent;
    }

    public ImageUri Icon => new ImageUri("Icons/icon_mini_table_big.png");
    public string TableName { get; }
    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public SchemaViewModel Schema { get; }
}