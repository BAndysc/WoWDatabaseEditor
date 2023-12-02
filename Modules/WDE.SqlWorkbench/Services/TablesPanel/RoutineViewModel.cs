using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MVVM;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.TablesPanel;

internal partial class RoutineViewModel : ObservableBase, INamedChildType
{
    public RoutineViewModel(SchemaViewModel parent, string routineName, RoutineType type)
    {
        RoutineName = routineName;
        Parent = parent;
        Schema = parent;
        if (type == RoutineType.Function)
            Icon = new ImageUri("Icons/icon_mini_func.png");
        else
            Icon = new ImageUri("Icons/icon_mini_proc.png");
    }

    public string Name => RoutineName;
    public ImageUri Icon { get; }
    public string RoutineName { get; }
    public bool CanBeExpanded => false;
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; }
    public IParentType? Parent { get; set; }
    public SchemaViewModel Schema { get; }
}