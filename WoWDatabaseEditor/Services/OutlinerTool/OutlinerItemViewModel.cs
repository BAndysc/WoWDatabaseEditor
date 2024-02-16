using System.Windows.Input;
using WDE.Common;
using WDE.Common.Outliner;
using WDE.Common.Types;
using WDE.Common.Utils;

namespace WoWDatabaseEditorCore.Services.OutlinerTool;

public partial class OutlinerItemViewModel : IChildType, IOutlinerItemViewModel
{
    public OutlinerItemViewModel(ISolutionItem? solutionItem, long? entry, string? name, ImageUri icon, ICommand? customCommand)
    {
        Name = name;
        Icon = icon;
        Entry = entry?.ToString();
        CustomCommand = customCommand;
        SolutionItem = solutionItem;
    }

    public ISolutionItem? SolutionItem { get; }
    public string? Name { get; }
    public ImageUri Icon { get; }
    public string? Entry { get; }
    public ICommand? CustomCommand { get; }
    public uint NestLevel { get; set; }
    public bool IsVisible { get; set; } = true;
    public IParentType? Parent { get; set; }
    public bool CanBeExpanded => false;
}