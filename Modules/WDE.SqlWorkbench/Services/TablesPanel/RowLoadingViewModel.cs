using WDE.Common.Utils;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class RowLoadingViewModel : IChildType
{
    public RowLoadingViewModel(SchemaViewModel parent)
    {
        Parent = parent;
    }

    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
}