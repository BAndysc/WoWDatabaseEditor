using System.Windows.Input;
using WDE.Common.Types;

namespace WDE.Common.Services.FindAnywhere;

public interface IFindAnywhereResult
{
    public ImageUri Icon { get; }
    public long? Entry { get; }
    public string Title { get; }
    public string Description { get; }
    public ISolutionItem? SolutionItem { get; }
    public ICommand? CustomCommand { get; }
}