using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FuzzySharp;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;

namespace WDE.QuestChainEditor.ViewModels;

public class QuestListItemViewModel
{
    public QuestListItemViewModel(uint entry, string title)
    {
        Entry = entry;
        Title = title;
    }

    public uint Entry { get; }
    public string Title { get; }
}

[AutoRegister]
[SingleInstance]
public class QuestPickerService
{
    private readonly IWindowManager windowManager;
    private readonly Func<QuestPickerViewModel> questPickerViewModelFactory;
    private QuestPickerViewModel? questPickerViewModel;

    private QuestPickerViewModel ViewModel
    {
        get
        {
            if (questPickerViewModel != null)
                return questPickerViewModel;
            questPickerViewModel = questPickerViewModelFactory();
            return questPickerViewModel;
        }
    }

    public QuestPickerService(IWindowManager windowManager, Func<QuestPickerViewModel> questPickerViewModelFactory)
    {
        this.windowManager = windowManager;
        this.questPickerViewModelFactory = questPickerViewModelFactory;
    }

    public async Task<uint?> PickQuest()
    {
        ViewModel.Reset();
        var result = await windowManager.ShowDialog(ViewModel);
        if (!result)
            return null;
        return ViewModel.SelectedQuest;
    }
}

public class QuestPickerViewModel : ObservableBase, IDialog
{
    private List<QuestListItemViewModel> quests;
    private Dictionary<uint, int> questById;
    private List<string> questNames;
    private string searchText = "";
    public ObservableCollection<QuestListItemViewModel> Quests { get; } = new();

    public string SearchText
    {
        get => searchText;
        set => SetProperty(ref searchText, value);
    }

    private CancellationTokenSource? filteringToken = null;
    public uint? SelectedQuest { get; set; }

    public QuestPickerViewModel(IDatabaseProvider databaseProvider)
    {
        quests = databaseProvider.GetQuestTemplates().Select(qt => new QuestListItemViewModel(qt.Entry, qt.Name))
            .ToList();
        questById = quests.Select((quest, index) => (quest, index)).ToDictionary(pair => pair.quest.Entry, pair => pair.index);
        questNames = quests.Select(q => q.Title.ToLower()).ToList();
        
        On(() => SearchText, searchText =>
        {
            if (filteringToken != null)
            {
                filteringToken.Cancel();
                filteringToken = null;
            }

            filteringToken = new CancellationTokenSource();
            DoFilter(searchText, filteringToken).ListenErrors();
        });

        CloseOkCommand = new DelegateCommand<uint?>(entry =>
        {
            SelectedQuest = entry;
            CloseOk?.Invoke();
        });

        CloseCancelCommand = new DelegateCommand(() =>
        {
            CloseCancel?.Invoke();
        });
    }

    public void Reset()
    {
        SearchText = "";
        SelectedQuest = null;
    }

    private async Task DoFilter(string searchText, CancellationTokenSource cancellationToken)
    {
        if (uint.TryParse(searchText, out var entryInt) && questById.TryGetValue(entryInt, out var questIndex))
        {
            if (filteringToken == cancellationToken)
            {
                Quests.Clear();
                Quests.Add(quests[questIndex]);
            }
            return;
        }
        
        await Task.Delay(50, cancellationToken.Token);

        if (cancellationToken.IsCancellationRequested)
            return;

        if (string.IsNullOrEmpty(searchText))
        {
            if (filteringToken == cancellationToken)
            {
                Quests.Clear();
                Quests.AddRange(quests);
            }
            return;
        }
        
        var results = await Task.Run(() => Process.ExtractSorted(searchText.ToLower(), questNames, null, null, 70));

        if (cancellationToken.IsCancellationRequested)
            return;

        if (filteringToken == cancellationToken)
        {
            Quests.Clear();
            foreach (var r in results)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;
            
                Quests.Add(quests[r.Index]);
            }
        }
    }

    public int DesiredWidth => 250;
    public int DesiredHeight => 500;
    public string Title => "Pick a quest";
    public bool Resizeable => true;

    public event Action? CloseCancel;
    public event Action? CloseOk;
    public DelegateCommand<uint?> CloseOkCommand { get; }
    public DelegateCommand CloseCancelCommand { get; }
}