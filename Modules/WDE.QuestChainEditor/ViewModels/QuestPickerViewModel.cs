using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData.Binding;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.QuestChainEditor.ViewModels;

[AutoRegister]
[SingleInstance]
public class QuestPickerViewModel : ObservableBase, IDialog
{
    private readonly IDatabaseProvider databaseProvider;
    private List<QuestListItemViewModel> quests;
    private Dictionary<uint, int> questById;
    private List<string> questNames;
    private string searchText = "";
    public ObservableCollectionExtended<QuestListItemViewModel> Quests { get; } = new();

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    private CancellationTokenSource? filteringToken = null;
    public uint? SelectedQuest { get; set; }

    private async Task LoadAsync()
    {
        quests = (await databaseProvider.GetQuestTemplatesAsync()).Select(qt => new QuestListItemViewModel(qt.Entry, qt.Name, qt.AllowableRaces, qt.AllowableClasses))
            .ToList();
        questById = quests.Select((quest, index) => (quest, index)).SafeToDictionary(pair => pair.quest.Entry, pair => pair.index);
        questNames = quests.Where(q => q.Title != null).Select(q => q.Title.ToLower()).ToList();
    }
    
    public QuestPickerViewModel(IDatabaseProvider databaseProvider)
    {
        quests = new();
        questById = new();
        questNames = new();
        this.databaseProvider = databaseProvider;
        LoadAsync().ListenErrors();

        On(() => SearchText, searchText =>
        {
            if (filteringToken != null)
            {
                filteringToken.Cancel();
                filteringToken = null;
            }

            filteringToken = new CancellationTokenSource();
            DoFilter(searchText, filteringToken.Token).ListenErrors();
        });

        CloseOkCommand = new DelegateCommand<uint?>(entry =>
        {
            SelectedQuest = entry;
            CloseOk?.Invoke();
        });

        Cancel = CloseCancelCommand = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });

        Accept = new DelegateCommand(() =>
        {
            CloseOk?.Invoke();
        }, () => SelectedQuest != null).ObservesProperty(() => SelectedQuest);
    }

    public void Reset()
    {
        SearchText = "";
        SelectedQuest = null;
    }

    private async Task DoFilter(string searchText, CancellationToken cancellationToken)
    {
        if (uint.TryParse(searchText, out var entryInt))
        {
            using var _ = Quests.SuspendNotifications();
            Quests.Clear();
            foreach (var quest in quests)
            {
                if (quest.Entry.Contains(searchText))
                    Quests.Add(quest);
            }
            return;
        }

        await Task.Delay(50, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return;

        if (string.IsNullOrEmpty(searchText))
        {
            using var _ = Quests.SuspendNotifications();
            Quests.Clear();
            Quests.AddRange(quests);
            return;
        }
        
        var results = await Task.Run(() =>
        {
            List<QuestListItemViewModel> output = new();
            foreach (var quest in quests)
            {
                if (quest.Entry.Contains(searchText) || quest.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    output.Add(quest);
            }
            return output;
        }, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return;

        using var __ = Quests.SuspendNotifications();
        Quests.Clear();
        Quests.AddRange(results);
    }

    public int DesiredWidth => 250;
    public int DesiredHeight => 500;
    public string Title => "Pick a quest";
    public bool Resizeable => true;
    public ICommand Accept { get; set; }
    public ICommand Cancel { get; set; }

    public event Action? CloseCancel;
    public event Action? CloseOk;
    public DelegateCommand<uint?> CloseOkCommand { get; }
    public DelegateCommand CloseCancelCommand { get; }
}