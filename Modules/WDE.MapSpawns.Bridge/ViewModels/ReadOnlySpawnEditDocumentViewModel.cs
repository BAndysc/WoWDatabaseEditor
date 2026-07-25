using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using WDE.Common;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Bridge.ViewModels;

/// <summary>
/// Base document for the 3D-world edit solution items (formations / spawn groups / waypoints).
/// These items are edited exclusively in the 3D map view; opening them from the session/solution
/// panel shows a READ-ONLY preview (plus the standard generate-query toolbar via
/// <see cref="ISolutionItemDocument.GenerateQuery"/>).
/// </summary>
public abstract class ReadOnlySpawnEditDocumentViewModel : BindableBase, ISolutionItemDocument
{
    private readonly ISolutionItemSqlGeneratorRegistry sqlRegistry;

    protected ReadOnlySpawnEditDocumentViewModel(ISolutionItem solutionItem, ISolutionItemSqlGeneratorRegistry sqlRegistry)
    {
        SolutionItem = solutionItem;
        this.sqlRegistry = sqlRegistry;
    }

    public ISolutionItem SolutionItem { get; }
    public abstract string Title { get; }
    public abstract ImageUri? Icon { get; }

    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public bool IsModified => false;
    public IHistoryManager? History => null;

    public Task<IQuery> GenerateQuery() => sqlRegistry.GenerateSql(SolutionItem);

    public void Dispose()
    {
    }
}
