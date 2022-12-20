using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.Services.FindAnywhere;

public class FindAnywhereResult : IFindAnywhereResult
{
    public FindAnywhereResult(ImageUri icon, long? entry, string title, string description, ISolutionItem? solutionItem = null, ICommand? command = null)
    {
        Entry = entry;
        Icon = icon;
        Title = title;
        Description = description;
        SolutionItem = solutionItem;
        CustomCommand = command;
    }

    public ImageUri Icon { get; }
    public long? Entry { get; set; }
    public string Title { get; }
    public string Description { get; }
    public ISolutionItem? SolutionItem { get; }
    public ICommand? CustomCommand { get; }
}