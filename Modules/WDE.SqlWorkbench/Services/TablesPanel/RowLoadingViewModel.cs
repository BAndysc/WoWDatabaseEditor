using WDE.Common.Types;
using WDE.Common.Utils;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class RowLoadingViewModel : INamedChildType
{
    public RowLoadingViewModel(SchemaViewModel parent)
    {
        Parent = parent;
    }

    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
    public string Name => "Loading...";
    public ImageUri Icon => ImageUri.Empty;
}