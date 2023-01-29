using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.EventScriptsEditor.Services;
using WDE.EventScriptsEditor.Solutions;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.EventScriptsEditor.ViewModels;

[AutoRegister]
public class EventScriptViewerViewModel : ObservableBase, IDocument
{
    private readonly IDatabaseProvider databaseProvider;
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title { get; }
    public ICommand Copy => AlwaysDisabledCommand.Command;
    public ICommand Cut=> AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ImageUri? Icon => new ImageUri("Icons/document_event_script.png");

    private EventScriptSolutionItem solutionItem;
    private readonly IEventScriptViewModelFactory viewModelFactory;

    public ObservableCollection<EventScriptLineViewModel> Lines { get; } = new();

    public EventScriptViewerViewModel(EventScriptSolutionItem solutionItem,
        ISolutionItemNameRegistry nameRegistry,
        IEventScriptViewModelFactory viewModelFactory,
        IDatabaseProvider databaseProvider)
    {
        this.solutionItem = solutionItem;
        this.viewModelFactory = viewModelFactory;
        this.databaseProvider = databaseProvider;
        Title = nameRegistry.GetName(solutionItem);
        Load().ListenErrors();
    }

    private async Task Load()
    {
        var lines = await databaseProvider.GetEventScript(solutionItem.ScriptType, solutionItem.Id);
        uint currentDelay = 0;
        foreach (var line in lines.OrderBy(l => l.Delay))
        {
            if (line.Delay > currentDelay)
            {
                var wait = (line.Delay - currentDelay);
                Lines.Add(new EventScriptLineViewModel("Wait " + wait + " second" + (wait != 1 ? "s" : ""), null));
                currentDelay = line.Delay;
            }
            
            Lines.Add(viewModelFactory.Factory(line));
        }
    }
}