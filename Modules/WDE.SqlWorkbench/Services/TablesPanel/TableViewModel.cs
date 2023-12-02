using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class TableViewModel : ObservableBase, INamedChildType
{
    public TableViewModel(SchemaViewModel parent, string tableName, TableType type)
    {
        TableName = tableName;
        Type = type;
        Parent = parent;
        Schema = parent;
        if (type == TableType.Table)
            Icon = new ImageUri("Icons/icon_mini_table_big.png");
        else
            Icon = new ImageUri("Icons/icon_mini_view_big.png");
    }

    public string Name => TableName;
    public ImageUri Icon { get; }
    public string TableName { get; }
    public TableType Type { get; }
    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public SchemaViewModel Schema { get; }
}