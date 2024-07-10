using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WoWDatabaseEditorCore.Services.FindAnywhere;

[AutoRegister]
public partial class FindAnywhereResultsViewModel : ObservableBase, IFindAnywhereResultsViewModel, IFindAnywhereResultContext
{
    private readonly IFindAnywhereService findAnywhereService;
    private readonly IEventAggregator eventAggregator;
    public ICommand Undo => AlwaysDisabledCommand.Command;
    public ICommand Redo => AlwaysDisabledCommand.Command;
    public IHistoryManager? History => null;
    public bool IsModified => false;
    public string Title => "Search results";
    public ICommand Copy  => AlwaysDisabledCommand.Command;
    public ICommand Cut  => AlwaysDisabledCommand.Command;
    public ICommand Paste  => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save  => AlwaysDisabledAsyncCommand.Command;
    public IAsyncCommand? CloseCommand { get; set; }
    public ImageUri? Icon => new ImageUri("Icons/document_search.png");
    public bool CanClose => true;

    public DelegateCommand<IFindAnywhereResult> OpenCommand { get; }

    public ObservableCollection<IFindAnywhereResult> Results { get; } = new();

    private CancellationTokenSource cancellationTokenSource = new();

    [Notify] private bool noResultsFound;
    [Notify] private bool searchingInProgress;
    [Notify] private string searchSummaryText = "";
    
    public FindAnywhereResultsViewModel(IFindAnywhereService findAnywhereService,
        IEventAggregator eventAggregator)
    {
        this.findAnywhereService = findAnywhereService;
        this.eventAggregator = eventAggregator;
        CloseCommand = new AsyncAutoCommand(() =>
        {
            cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        });
        OpenCommand = new DelegateCommand<IFindAnywhereResult>(result =>
        {
            if (result.SolutionItem != null)
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(result.SolutionItem);
            }
            else if (result.CustomCommand != null)
            {
                result.CustomCommand?.Execute(result);
            }
        });
    }
    
    public async Task Search(IReadOnlyList<string> parameter, IReadOnlyList<long> values)
    {
        SearchingInProgress = true;
        if (values.Count == 1)
            SearchSummaryText = parameter[0].Replace("Parameter", "") + " = " + values[0];
        else
            SearchSummaryText = parameter[0].Replace("Parameter", "") + " IN (" + string.Join(", ", values) + ")";
        await findAnywhereService.Find(this, FindAnywhereSourceType.All, parameter, values, cancellationTokenSource.Token);
        SearchingInProgress = false;
        if (Results.Count == 0)
            NoResultsFound = true;
    }

    public void AddResult(IFindAnywhereResult result)
    {
        Results.Add(result);
    }
}