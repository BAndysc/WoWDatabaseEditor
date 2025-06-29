using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Avalonia;
using DynamicData.Binding;
using Prism.Commands;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Exceptions;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.QuestChainEditor.Documents;
using WDE.QuestChainEditor.GraphLayouting;
using WDE.QuestChainEditor.GraphLayouting.ViewModels;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.QueryGenerators;
using WDE.QuestChainEditor.Services;
using WDE.QuestChainEditor.Views;
using WDE.SqlQueryGenerator;

namespace WDE.QuestChainEditor.ViewModels;

[AutoRegister]
public partial class QuestChainDocumentViewModel : ObservableBase, ISolutionItemDocument, IProblemSourceDocument, IWindowViewModel, ISolutionItemManualUpdateSessionOnSave
{
    [Notify] private Point viewportLocation; // must be bound or else BringToView animation won't work
    [Notify] private bool isSearchBoxVisible;
    [Notify] private bool shiftAltConnectionMessageVisible;
    [Notify] private bool shiftAltConnectionTeachingTipVisible;

    private readonly IQuestTemplateSource questTemplateSource;
    private readonly IChainGenerator chainGenerator;
    private readonly IQueryGenerator queryGenerator;
    private readonly IMessageBoxService messageBoxService;
    private readonly IQuestChainLoader questChainLoader;
    private readonly IAutomaticGraphLayouter automaticGraphLayouter;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly IMainThread mainThread;
    private readonly IConditionEditService conditionEditService;
    private readonly IQuestChainEditorConfiguration configuration;
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly QuestPickerService questPickerService;
    private readonly IQuestChainEditorPreferences userPreferences;
    private readonly ITeachingTipService teachingTipService;
    private readonly QuestChainSolutionItem solutionItem;

    private HistoryHandler historyHandler;
    public HistoryHandler HistoryHandler => historyHandler;

    private bool hasQuestStatus;

    public ObservableCollectionExtended<BaseQuestViewModel> Elements { get; } = new();
    public ObservableCollectionExtended<ConnectionViewModel> Connections { get; } = new();
    public ObservableCollection<ConnectionViewModel> VisibleConnections { get; } = new();
    public ObservableCollectionExtended<ConnectionViewModel> SelectedConnections { get; } = new();
    public ObservableCollectionExtended<BaseQuestViewModel> SelectedItems { get; } = new();

    public IReadOnlyList<ExclusiveGroupViewModel> SelectedGroupsSnapshot() => SelectedItems.Select(x => x as ExclusiveGroupViewModel).Where(x => x != null).Select(x => x!).ToList();

    public IReadOnlyList<QuestViewModel> SelectedQuestsSnapshot() => SelectedItems.Select(x => x as QuestViewModel).Where(x => x != null).Select(x => x!).ToList();

    public IReadOnlyList<ConnectionViewModel> SelectedConnectionsSnapshot() => SelectedConnections.ToList();

    private Dictionary<uint, QuestViewModel> entryToQuest = new();
    private Dictionary<uint, ChainRawData> existingData = new();
    private Dictionary<uint, ChainRawData> removedQuestsToReSave = new();
    private Dictionary<uint, IReadOnlyList<ICondition>> removedQuestConditionsToReSave = new();
    private Dictionary<uint, IReadOnlyList<ICondition>> existingConditions = new();

    public PendingConnectionViewModel PendingConnection { get; } = new();

    public DelegateCommand<ExclusiveGroupViewModel> ToggleExclusiveGroupTypeCommand { get; }
    public DelegateCommand SetQuestHordeOnlyCommand { get; }
    public DelegateCommand SetQuestAllianceOnlyCommand { get; }
    public DelegateCommand<QuestViewModel> SetQuestAnyTeamCommand { get; }
    public DelegateCommand<ConnectionViewModel> SetConnectionRequiredCommand { get; }
    public DelegateCommand<ConnectionViewModel> SetConnectionBreadcrumbCommand { get; }
    public DelegateCommand<ConnectionViewModel> SetConnectionMustBeActiveCommand { get; }
    public AsyncAutoCommand<QuestViewModel> PickQuestClassesCommand { get; }
    public AsyncAutoCommand<QuestViewModel> PickQuestRacesCommand { get; }
    public AsyncCommand<QuestViewModel> EditShowQuestMarkConditions { get; }
    public DelegateCommand GroupSelectedQuestsCommand { get; }
    public DelegateCommand MoveOutgoingConnectionsToGroupCommand { get; }
    public DelegateCommand MoveIncomingConnectionsToGroupCommand { get; }
    public IAsyncCommand EditTemplateCommand { get; }
    public IAsyncCommand EditCreatureStartersCommand { get; }
    public IAsyncCommand EditCreatureQuestEndersCommand { get; }
    public IAsyncCommand EditGameobjectStartersCommand { get; }
    public IAsyncCommand EditGameobjectQuestEndersCommand { get; }
    public DelegateCommand DoLayoutGraphCommand { get; }
    public DelegateCommand DeleteOutgoingConnectionsCommand { get; }
    public DelegateCommand DeleteIncomingConnectionsCommand { get; }
    public DelegateCommand DeleteSelectedExclusiveGroupsCommand { get; }
    public DelegateCommand EvaluateQuestStatusCommand { get; }
    public IAsyncCommand AddQuestToPlayerCommand { get; }
    public IAsyncCommand RemoveQuestFromPlayerCommand { get; }
    public IAsyncCommand CompleteQuestForPlayerCommand { get; }
    public IAsyncCommand RewardQuestForPlayerCommand { get; }
    public DelegateCommand<QuestViewModel> OpenWowHeadCommand { get; }
    public DelegateCommand<QuestViewModel> OpenShootCommand { get; }
    public DelegateCommand ToggleSearchBoxCommand { get; }
    public IAsyncCommand<QuestViewModel> UnloadThisChainCommand { get; }
    public DelegateCommand SelectAllCommand { get; }
    public DelegateCommand CopyAllEntries { get; }
    public IAsyncCommand LoadQuestsBySortIdCommand { get; }

    private bool TryGetQuest(uint entry, out QuestViewModel quest)
    {
        if (entryToQuest.TryGetValue(entry, out var quest_))
        {
            quest = quest_;
            return true;
        }
        quest = null!;
        return false;
    }

    public QuestChainDocumentViewModel(QuestChainSolutionItem solutionItem,
        IHistoryManager history,
        IQuestTemplateSource questTemplateSource,
        IChainGenerator chainGenerator,
        IQueryGenerator queryGenerator,
        ITaskRunner taskRunner,
        IMySqlExecutor mySqlExecutor,
        ICachedDatabaseProvider databaseProvider,
        QuestPickerViewModel questPicker,
        IMessageBoxService messageBoxService,
        IQuestChainLoader questChainLoader,
        IAutomaticGraphLayouter automaticGraphLayouter,
        IClipboardService clipboardService,
        ICurrentCoreVersion currentCoreVersion,
        IMainThread mainThread,
        IConditionEditService conditionEditService,
        IQuestChainEditorConfiguration configuration,
        IParameterPickerService parameterPickerService,
        IStandaloneTableEditService tableEditService,
        IQuestChainsServerIntegrationService questChainsServerIntegrationService,
        IItemFromListProvider itemFromListProvider,
        QuestPickerService questPickerService,
        IWindowManager windowManager,
        ISessionService sessionService,
        IQuestChainEditorPreferences userPreferences,
        ITeachingTipService teachingTipService,
        GraphLayoutSettingsViewModel layoutSettingsViewModel)
    {
        this.questTemplateSource = questTemplateSource;
        this.chainGenerator = chainGenerator;
        this.queryGenerator = queryGenerator;
        this.messageBoxService = messageBoxService;
        this.questChainLoader = questChainLoader;
        this.automaticGraphLayouter = automaticGraphLayouter;
        this.currentCoreVersion = currentCoreVersion;
        this.mainThread = mainThread;
        this.conditionEditService = conditionEditService;
        this.configuration = configuration;
        this.itemFromListProvider = itemFromListProvider;
        this.questPickerService = questPickerService;
        this.userPreferences = userPreferences;
        this.teachingTipService = teachingTipService;
        QuestPicker = questPicker;
        QuestChainsServerIntegrationService = questChainsServerIntegrationService;
        LayoutSettingsViewModel = layoutSettingsViewModel;
        SolutionItem = this.solutionItem = solutionItem;
        RemoteCommandsSupported = QuestChainsServerIntegrationService.IsSupported;
        existingData = solutionItem.ExistingData.SafeToDictionary(e => e.Key, e=>e.Value);
        existingConditions = solutionItem.ExistingConditions
            .Where(x => x.Value != null && x.Value.Count > 0)
            .SafeToDictionary(x => x.Key, x => (IReadOnlyList<ICondition>)x.Value.Select(c => (ICondition)c).ToList());
        autoLayout = userPreferences.AutoLayout;
        hideFactionChangeArrows = userPreferences.HideFactionChangeArrows;

        Connections.ToStream(false)
            .Subscribe(change =>
            {
                if (change.Type == CollectionEventType.Add)
                {
                    if (!hideFactionChangeArrows || change.Item.RequirementType != ConnectionType.FactionChange)
                        VisibleConnections.Add(change.Item);
                }
                else if (change.Type == CollectionEventType.Remove)
                {
                    VisibleConnections.Remove(change.Item);
                }
            });
        this.ToObservable(() => HideFactionChangeArrows)
            .Subscribe(hideFactionChangeArrows =>
            {
                if (hideFactionChangeArrows)
                {
                    foreach (var conn in VisibleConnections.ToList())
                    {
                        if (conn.RequirementType == ConnectionType.FactionChange)
                            VisibleConnections.Remove(conn);
                    }
                }
                else
                {
                    var connSet = new HashSet<ConnectionViewModel>(VisibleConnections);
                    foreach (var conn in Connections)
                    {
                        if (conn.RequirementType == ConnectionType.FactionChange)
                        {
                            if (!connSet.Contains(conn))
                                VisibleConnections.Add(conn);
                        }
                    }
                }
            });

        if (sessionService.CurrentSession is { } currentSession &&
            !sessionService.IsPaused)
        {
            var sessionSolutionItem = currentSession.Find(solutionItem) as QuestChainSolutionItem;
            if (sessionSolutionItem != null)
            {
                foreach (var (questId, sessionExistingData) in sessionSolutionItem.ExistingData)
                {
                    existingData[questId] = sessionExistingData;
                }

                foreach (var (questId, sessionCondition) in sessionSolutionItem.ExistingConditions)
                {
                    if (sessionCondition.Count > 0)
                        existingConditions[questId] = sessionCondition;
                }
            }
        }

        History = history;
        DatabaseProvider = databaseProvider;
        Undo = history.UndoCommand();
        Redo = history.RedoCommand();
        historyHandler = new HistoryHandler();
        history.AddHandler(historyHandler);

        Elements.ToCountChangedObservable().SubscribeAction(_ => RaisePropertyChanged(nameof(IsEmpty)));

        void RelayoutGraph() => ReLayoutGraph(Elements, Connections);

        layoutSettingsViewModel.SettingsChanged += RelayoutGraph;
        AutoDispose(new ActionDisposable(() => layoutSettingsViewModel.SettingsChanged -= RelayoutGraph));

        SelectedItems.CollectionChanged += (_, __) => UpdateIsHighlighted();

        History.OnRedo += () =>
        {
            RefreshProblems();
            ReLayoutGraph(Elements, Connections);
        };
        History.OnUndo += () =>
        {
            RefreshProblems();
            ReLayoutGraph(Elements, Connections);
        };

        QuestPicker.CloseCancel += OnCancelQuestPicker;
        QuestPicker.CloseOk += OnOkQuestPicker;

        AutoDispose(new ActionDisposable(() =>
        {
            QuestPicker.CloseCancel -= OnCancelQuestPicker;
            QuestPicker.CloseOk -= OnOkQuestPicker;
        }));

        ToggleSearchBoxCommand = new DelegateCommand(() =>
        {
            IsSearchBoxVisible = !IsSearchBoxVisible;
        });

        Save = new AsyncAutoCommand(async () =>
        {
            var store = await BuildCurrentQuestStore();
            var newConditions = store.Where(x => x.Conditions.Count > 0).ToDictionary(x => x.Entry, x => x.Conditions);
            var query = await chainGenerator.Generate(store, existingData);
            var sql = queryGenerator.GenerateQuery(query.Concat(removedQuestsToReSave.Values).ToList(), null, newConditions, null); // passing null as 'existing' to generate all
            await taskRunner.ScheduleTask("Save chain", async () =>
            {
                await mySqlExecutor.ExecuteSql(await sql);
                removedQuestsToReSave.Clear();
                removedQuestConditionsToReSave.Clear();
                history.MarkAsSaved();
                if (sessionService.CurrentSession is { } currentSession)
                {
                    var sessionSolutionItem = currentSession.Find(solutionItem) as QuestChainSolutionItem ?? new QuestChainSolutionItem();
                    foreach (var (quest, data) in existingData)
                    {
                        if (existingConditions.TryGetValue(quest, out var existingCondition) && existingCondition.Count > 0)
                            sessionSolutionItem.UpdateEntry(quest, data, existingCondition);
                        else
                            sessionSolutionItem.UpdateEntry(quest, data, null);
                    }
                    await sessionService.UpdateQuery(sessionSolutionItem);
                }
            });
        });

        Copy = new DelegateCommand(() =>
        {
            if (SelectedItems.Count > 0)
            {
                clipboardService.SetText(string.Join(", ", SelectedItems.Where(x => x is QuestViewModel)
                    .Select(x => x.EntryOrExclusiveGroupId)));
            }
        });

        ToggleExclusiveGroupTypeCommand = new DelegateCommand<ExclusiveGroupViewModel>(group =>
        {
            var newType = group.IsAnyGroupType ? QuestGroupType.All : QuestGroupType.OneOf;
            SetExclusiveGroupsType(SelectedGroupsSnapshot(), newType);
        });

        SetQuestHordeOnlyCommand = new DelegateCommand(() =>
        {
            SetQuestsRace(SelectedQuestsSnapshot(), currentCoreVersion.Current.GameVersionFeatures.AllRaces & CharacterRaces.AllHorde);
        });

        SetQuestAllianceOnlyCommand = new DelegateCommand(() =>
        {
            SetQuestsRace(SelectedQuestsSnapshot(), currentCoreVersion.Current.GameVersionFeatures.AllRaces & CharacterRaces.AllAlliance);
        });

        SetQuestAnyTeamCommand = new DelegateCommand<QuestViewModel>(quest =>
        {
            SetQuestsRace(SelectedQuestsSnapshot(), 0);
        });

        SetConnectionRequiredCommand = new DelegateCommand<ConnectionViewModel>(conn =>
        {
            SetConnectionsType(SelectedConnectionsSnapshot(), QuestRequirementType.Completed);
        });

        SetConnectionBreadcrumbCommand = new DelegateCommand<ConnectionViewModel>(conn =>
        {
            SetConnectionsType(SelectedConnectionsSnapshot(), QuestRequirementType.Breadcrumb);
        });

        SetConnectionMustBeActiveCommand = new DelegateCommand<ConnectionViewModel>(conn =>
        {
            SetConnectionsType(SelectedConnectionsSnapshot(), QuestRequirementType.MustBeActive);
        });

        PickQuestClassesCommand = new AsyncAutoCommand<QuestViewModel>(async q =>
        {
            var (classes, ok) = await parameterPickerService.PickParameter("ClassMaskParameter", (long)q.Classes);
            if (ok)
                SetQuestsClasses(SelectedQuestsSnapshot(), (CharacterClasses)classes);
        });

        PickQuestRacesCommand = new AsyncAutoCommand<QuestViewModel>(async q =>
        {
            var (races, ok) = await parameterPickerService.PickParameter("RaceMaskParameter", (long)q.Races);
            if (ok)
                SetQuestsRace(SelectedQuestsSnapshot(), (CharacterRaces)races);
        });

        EditShowQuestMarkConditions = new AsyncCommand<QuestViewModel>(async q =>
        {
            if (this.configuration.ShowMarkConditionSourceType is not { } cond)
                return;

            var newConditions = await conditionEditService.EditConditions(new IDatabaseProvider.ConditionKey(cond, null, (int)q!.Entry, null), q.Conditions);

            if (newConditions == null)
                return;

            var conditions = newConditions.ToList();
            SetConditionsForQuests(SelectedQuestsSnapshot(), conditions);
        }, _ => this.configuration.ShowMarkConditionSourceType.HasValue);

        EditTemplateCommand = new AsyncCommand(async () =>
        {
            var selectedQuests = SelectedQuestsSnapshot();
            if (selectedQuests.Count > 0)
                tableEditService.OpenTemplatesEditor(selectedQuests.Select(q => new DatabaseKey(q.Entry)).ToList(), DatabaseTable.WorldTable("quest_template"));
        }).WrapMessageBox<Exception>(this.messageBoxService);

        EditCreatureStartersCommand = new AsyncCommand(async () =>
        {
            var selectedQuests = SelectedQuestsSnapshot();
            if (selectedQuests.Count > 0)
            {
                try
                {
                    tableEditService.OpenMultiRecordEditor(
                        selectedQuests.Select(q => new DatabaseKey(q.Entry)).ToList(),
                        DatabaseTable.WorldTable("creature_queststarter"),
                        DatabaseTable.WorldTable("creature_quest_starter"),
                        DatabaseTable.WorldTable("creature_questrelation"));
                }
                catch (UnsupportedTableException)
                {
                    tableEditService.OpenEditor(DatabaseTable.WorldTable("creature_questrelation"), null, "`quest` IN (" + string.Join(",", selectedQuests.Select(q => q.Entry)) + ")");
                }
            }
        }).WrapMessageBox<Exception>(this.messageBoxService);

        EditCreatureQuestEndersCommand = new AsyncCommand(async () =>
        {
            var selectedQuests = SelectedQuestsSnapshot();
            if (selectedQuests.Count > 0)
            {
                try
                {
                    tableEditService.OpenMultiRecordEditor(
                        selectedQuests.Select(q => new DatabaseKey(q.Entry)).ToList(),
                        DatabaseTable.WorldTable("creature_questender"),
                        DatabaseTable.WorldTable("creature_quest_ender"),
                        DatabaseTable.WorldTable("creature_involvedrelation"));
                }
                catch (UnsupportedTableException)
                {
                    tableEditService.OpenEditor(DatabaseTable.WorldTable("creature_involvedrelation"), null, "`quest` IN (" + string.Join(",", selectedQuests.Select(q => q.Entry)) + ")");
                }
            }
        }).WrapMessageBox<Exception>(this.messageBoxService);

        EditGameobjectStartersCommand = new AsyncCommand(async () =>
        {
            var selectedQuests = SelectedQuestsSnapshot();
            if (selectedQuests.Count > 0)
            {
                try
                {
                    tableEditService.OpenMultiRecordEditor(
                        selectedQuests.Select(q => new DatabaseKey(q.Entry)).ToList(),
                        DatabaseTable.WorldTable("gameobject_queststarter"),
                        DatabaseTable.WorldTable("gameobject_questrelation"));
                }
                catch (UnsupportedTableException)
                {
                    tableEditService.OpenEditor(DatabaseTable.WorldTable("gameobject_questrelation"), null, "`quest` IN (" + string.Join(",", selectedQuests.Select(q => q.Entry)) + ")");
                }
            }
        }).WrapMessageBox<Exception>(this.messageBoxService);

        EditGameobjectQuestEndersCommand = new AsyncCommand(async () =>
        {
            var selectedQuests = SelectedQuestsSnapshot();
            if (selectedQuests.Count > 0)
            {
                try
                {
                    tableEditService.OpenMultiRecordEditor(
                        selectedQuests.Select(q => new DatabaseKey(q.Entry)).ToList(),
                        DatabaseTable.WorldTable("gameobject_questender"),
                        DatabaseTable.WorldTable("gameobject_involvedrelation"));
                }
                catch (UnsupportedTableException)
                {
                    tableEditService.OpenEditor(DatabaseTable.WorldTable("gameobject_involvedrelation"), null, "`quest` IN (" + string.Join(",", selectedQuests.Select(q => q.Entry)) + ")");
                }
            }
        }).WrapMessageBox<Exception>(this.messageBoxService);

        EvaluateQuestStatusCommand = new DelegateCommand(() =>
        {
            EvaluateQuestStatus().ListenErrors(messageBoxService);
        });

        AddQuestToPlayerCommand = new AsyncCommand(async () =>
        {
            if (await PickPlayerName() is not { } playerName)
                return;

            await questChainsServerIntegrationService.AddQuests(playerName, SelectedQuestsSnapshot().Select(q => q.Entry).ToList());
            if (hasQuestStatus)
                await EvaluateQuestStatus();
        }).WrapMessageBox<Exception>(messageBoxService);

        RemoveQuestFromPlayerCommand = new AsyncCommand(async () =>
        {
            if (await PickPlayerName() is not { } playerName)
                return;

            await questChainsServerIntegrationService.RemoveQuests(playerName, SelectedQuestsSnapshot().Select(q => q.Entry).ToList());
            if (hasQuestStatus)
                await EvaluateQuestStatus();
        }).WrapMessageBox<Exception>(messageBoxService);

        CompleteQuestForPlayerCommand = new AsyncCommand(async () =>
        {
            if (await PickPlayerName() is not { } playerName)
                return;

            await questChainsServerIntegrationService.CompleteQuests(playerName, SelectedQuestsSnapshot().Select(q => q.Entry).ToList());
            if (hasQuestStatus)
                await EvaluateQuestStatus();
        }).WrapMessageBox<Exception>(messageBoxService);

        RewardQuestForPlayerCommand = new AsyncCommand(async () =>
        {
            if (await PickPlayerName() is not { } playerName)
                return;

            await questChainsServerIntegrationService.RewardQuests(playerName, SelectedQuestsSnapshot().Select(q => q.Entry).ToList());
            if (hasQuestStatus)
                await EvaluateQuestStatus();
        }).WrapMessageBox<Exception>(messageBoxService);

        GroupSelectedQuestsCommand = new DelegateCommand(() =>
        {
            CreateAndGroupSelectedQuests(null);
        });

        MoveOutgoingConnectionsToGroupCommand = new DelegateCommand(() =>
        {
            MoveConnectionsToGroup(true, SelectedQuestsSnapshot());
        });

        MoveIncomingConnectionsToGroupCommand = new DelegateCommand(() =>
        {
            MoveConnectionsToGroup(false, SelectedQuestsSnapshot());
        });

        StartConnectionCommand = new DelegateCommand(() =>
        {
            PendingConnection.IsVisible = true;
        });

        DeleteOutgoingConnectionsCommand = new DelegateCommand(() =>
        {
            var connections = SelectedItems
                .SelectMany(q => q.RequirementFor.Select(tuple => tuple.conn))
                .ToList();
            DeleteConnections(connections);
        });

        DeleteIncomingConnectionsCommand = new DelegateCommand(() =>
        {
            var connections = SelectedItems
                .SelectMany(q => q.Prerequisites.Select(tuple => tuple.conn))
                .ToList();
            DeleteConnections(connections);
        });

        CreateConnectionCommand = new AsyncAutoCommand(async () =>
        {
            var to = PendingConnection.To;
            if (to == null)
            {
                var quest = await PickQuest();
                if (!quest.HasValue)
                    return;

                to = await LoadQuestWithDependencies(quest.Value, PendingConnection.TargetLocation, false);
            }

            AddConnection(PendingConnection.From!, to, PendingConnection.RequirementType);
        }, () => PendingConnection.From != null && PendingConnection.From != PendingConnection.To);

        DeleteSelectedExclusiveGroupsCommand = new DelegateCommand(() =>
        {
            DeleteGroups(SelectedGroupsSnapshot());
        });

        DeleteSelectedCommand = new DelegateCommand(() =>
        {
            DeleteSelectedConnections();
            DeleteGroups(SelectedGroupsSnapshot());
        });

        OpenWowHeadCommand = new DelegateCommand<QuestViewModel>(quest =>
        {
            string exp = currentCoreVersion.Current.Version.Major switch
            {
                1 => "wotlk/",
                2 => "wotlk/",
                3 => "wotlk/",
                4 => "cataclysm/",
                _ => ""
            };
            var url = $"https://wowhead.com/{exp}quest={quest.Entry}";
            windowManager.OpenUrl(url);
        });

        OpenShootCommand = new DelegateCommand<QuestViewModel>(quest =>
        {
            string exp = currentCoreVersion.Current.Version.Major switch
            {
                1 => "",
                2 => "",
                3 => "",
                4 => "cata-",
                5 => "mop-",
                6 => "legion-",
                7 => "legion-",
                _ => ""
            };
            var url = $"https://{exp}shoot.tauri.hu/?quest={quest.Entry}";
            windowManager.OpenUrl(url);
        });

        UnloadThisChainCommand = new AsyncCommand<QuestViewModel>(async quest =>
        {
            await AskToSave();
            UnloadConnectedNodesAndConnections(quest!);
        }).WrapMessageBox<Exception, QuestViewModel>(messageBoxService);

        DoLayoutGraphCommand = new DelegateCommand(DoLayoutGraphNow);

        SelectAllCommand = new DelegateCommand(() =>
        {
            using var _ = SelectedItems.SuspendNotifications();
            SelectedConnections.Clear();
            SelectedItems.AddRange(Elements);
        });

        CopyAllEntries = new DelegateCommand(() =>
        {
            clipboardService.SetText(string.Join(", ", Elements.OfType<QuestViewModel>().Select(x => x.Entry)));
        });

        LoadQuestsBySortIdCommand = new AsyncCommand(async () =>
        {
            var (zoneId, ok) = await parameterPickerService.PickParameter("ZoneOrQuestSortParameter");
            if (!ok)
                return;

            var quests = await databaseProvider.GetQuestTemplatesBySortIdAsync((int)zoneId);

            quests = quests.Where(q => !q.Flags.HasFlagFast(QuestFlags.TrackingEvent)).ToList();

            if (quests.Count == 0)
            {
                await messageBoxService.SimpleDialog("Info", "No quests found", "No quests found in the database for the selected zone or sort id");
                return;
            }

            await ExecuteTask(token => LoadQuestWithDependencies(quests.Select(x => x.Entry).ToArray(), default, true, token));
        });

        History.ToObservable(x => x.IsSaved).SubscribeAction(_ => RaisePropertyChanged((nameof(IsModified))));
        //LoadQuestWithDependencies(25134, default).ListenErrors();

        if (solutionItem.Entries.Count > 0)
        {
            LoadingStack++;
            taskRunner.ScheduleTask("Load quests", async () =>
            {
                foreach (var quest in solutionItem.Entries)
                    await LoadQuestWithDependencies(quest, default, true);
                LoadingStack--;
                History.Clear();
            });
        }

        if (this.teachingTipService.ShowTip("QUEST_CHAIN_BETA"))
        {
            messageBoxService.SimpleDialog("Quest chain editor", "Quest chain editor is a beta version", "This quest chain editor has been tested, but it still may contain bugs, please pay attention to the generate queries until it is well tested.");
        }
    }

    public async Task AskToSave()
    {
        if (!History.IsSaved)
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<SaveDialogResult>()
                .SetTitle("Save changes?")
                .SetMainInstruction("You have unsaved changes")
                .SetContent("Do you want to save the changes before unloading the chain?")
                .WithYesButton(SaveDialogResult.Save)
                .WithNoButton(SaveDialogResult.DontSave)
                .WithCancelButton(SaveDialogResult.Cancel)
                .Build());

            if (result == SaveDialogResult.Save)
                await Save.ExecuteAsync();

            if (result == SaveDialogResult.Cancel)
                throw new TaskCanceledException();
        }
    }

    private void OnOkQuestPicker()
    {
        if (!isPickingQuest)
            return;

        questPickingTask?.SetResult(QuestPicker.SelectedQuest);
        questPickingTask = null;
        isPickingQuest = false;
        RaisePropertyChanged(nameof(IsPickingQuest));
    }

    private void OnCancelQuestPicker()
    {
        if (!isPickingQuest)
            return;

        questPickingTask?.SetResult(null);
        questPickingTask = null;
        isPickingQuest = false;
        RaisePropertyChanged(nameof(IsPickingQuest));
    }

    public void UpdateIsHighlighted()
    {
        foreach (var quest in Elements.Where(x => x is QuestViewModel).Cast<QuestViewModel>())
        {
            quest.IsHighlighted = quest.FactionChange is {} factionChange &&
                                  !SelectedItems.Contains(quest) &&
                                  SelectedItems.Contains(factionChange.quest);
        }
    }

    public async Task<QuestViewModel> LoadQuestWithDependencies(uint quest, Point targetLocation, bool navigateToQuest)
    {
        if (entryToQuest.TryGetValue(quest, out var existingQuestViewModel))
            return existingQuestViewModel;

        return await ExecuteTask(token => LoadQuestWithDependencies([quest], targetLocation, navigateToQuest, token));
    }

    public async Task<QuestViewModel> LoadQuestWithDependencies(uint[] quests, Point targetLocation, bool navigateToQuest, CancellationToken token)
    {
        Dictionary<uint, QuestViewModel> addedQuestsByEntry = new();
        List<BaseQuestViewModel> addedQuests = new();
        List<ConnectionViewModel> addedConnections = new();
        List<ChainRawData> newExistingData = new();
        List<(uint, IReadOnlyList<ICondition>)> newExistingConditions = new();
        List<string> nonFatalErrors = new();
        Dictionary<int, ExclusiveGroupViewModel> groups = new();

        LoadingStack++;
        try
        {
            List<QuestViewModel> mainQuests = new();

            var questStore = new QuestStore();
            await questChainLoader.LoadChain(quests.ToArray(), questStore, nonFatalErrors, token);

            token.ThrowIfCancellationRequested();

            QuestViewModel GetOrCreate(IQuestTemplate template)
            {
                if (TryGetQuest(template.Entry, out var vm))
                    return vm;

                if (addedQuestsByEntry.TryGetValue(template.Entry, out var vm2))
                    return vm2;

                vm = addedQuestsByEntry[template.Entry] = new QuestViewModel(this, template, currentCoreVersion)
                {
                    Bounds = new Rect(0, 0, 120, 50)
                };
                addedQuests.Add(vm);
                return vm;
            }

            foreach (var q in questStore)
            {
                if (q.Template == null)
                    throw new Exception($"Quest {q.Entry} is referenced, but it doesn't exist in the database. Please fix the database.");

                var viewModel = GetOrCreate(q.Template);

                viewModel.Conditions = q.Conditions;

                if (quests.Contains(q.Entry))
                    mainQuests.Add(viewModel);

                void LoadGroups(QuestRequirementType type, IReadOnlyList<IQuestGroup> questGroups)
                {
                    foreach (var requirementGroup in questGroups)
                    {
                        ExclusiveGroupViewModel? group = null;
                        if (requirementGroup.Quests.Count > 1 &&
                            requirementGroup.GroupId != 0 &&
                            !groups.TryGetValue(requirementGroup.GroupId, out group))
                        {
                            group = new ExclusiveGroupViewModel(this, requirementGroup.GroupType)
                            {
                                Size = new Size(100, 100)
                            };
                            addedQuests.Add(group);
                            groups[requirementGroup.GroupId] = group;
                        }

                        foreach (var requiredQuestEntry in requirementGroup.Quests)
                        {
                            var requiredQuest = GetOrCreate(questStore[requiredQuestEntry].Template);

                            group?.AddQuest(requiredQuest);

                            if (group != null)
                            {
                                if (group.RequirementFor.All(conn => conn.Item1 != viewModel))
                                {
                                    var connection = new ConnectionViewModel(type.ToConnectionType(), group, viewModel);
                                    addedConnections.Add(connection);
                                }
                            }
                            else
                            {
                                var connection = new ConnectionViewModel(type.ToConnectionType(), requiredQuest, viewModel);
                                addedConnections.Add(connection);
                            }
                        }
                    }
                }
                LoadGroups(QuestRequirementType.Completed, q.MustBeCompleted);
                LoadGroups(QuestRequirementType.MustBeActive, q.MustBeActive);
                LoadGroups(QuestRequirementType.Breadcrumb, q.Breadcrumbs);
            }

            foreach (var (groupType, questIds) in questStore.GetAdditionalGroups())
            {
                var group = new ExclusiveGroupViewModel(this, groupType);
                addedQuests.Add(group);
                foreach (var questId in questIds)
                {
                    var questViewModel = GetOrCreate(questStore[questId].Template);
                    if (questViewModel.ExclusiveGroup != null)
                        throw new Exception("Cannot add quest to group, it already belongs to another group");
                    group.AddQuest(questViewModel);
                }
            }

            foreach (var element in addedQuests)
            {
                if (element is ExclusiveGroupViewModel groupViewModel)
                {
                    if (groupViewModel.Quests.Count == 0 || !groupViewModel.Quests[0].Prerequisites.Any())
                        continue;

                    var requires = groupViewModel.Quests[0].Prerequisites.First();
                    var originalFrom = requires.Item1;
                    if (groupViewModel.Quests.All(q => q.Prerequisites.Count() == 1 && q.Prerequisites.First() is {} first && first.prerequisite == requires.prerequisite && first.requirementType == requires.requirementType))
                    {
                        foreach (var questInGroup in groupViewModel.Quests)
                        {
                            for (var index = questInGroup.Connector.Connections.Count - 1; index >= 0; index--)
                            {
                                var conn = questInGroup.Connector.Connections[index];
                                if (conn.ToNode == questInGroup)
                                {
                                    conn.Detach();
                                    addedConnections.Remove(conn);
                                }
                            }
                        }

                        var connectionToGroup = new ConnectionViewModel(requires.Item2.ToConnectionType(), originalFrom, groupViewModel);
                        addedConnections.Add(connectionToGroup);
                    }
                }
            }

            foreach (var quest in questStore)
            {
                if (quest.OtherFactionQuest is not {} otherFactionQuest)
                    continue;

                var questVm = GetOrCreate(quest.Template);
                var otherQuestVm = GetOrCreate(questStore[otherFactionQuest.Id].Template);

                if (otherQuestVm.OtherFactionQuest is {} otherOtherQuest)
                {
                    Debug.Assert(otherOtherQuest.Entry == questVm.Entry);
                    continue;
                }

                if (questVm.OtherFactionQuest is { } otherQuest)
                {
                    Debug.Assert(otherQuest.Entry == otherQuestVm.Entry);
                    continue;
                }

                var conn = new ConnectionViewModel(ConnectionType.FactionChange, questVm, otherQuestVm);
                addedConnections.Add(conn);
            }

            foreach (var group in addedQuests.OfType<ExclusiveGroupViewModel>())
                group.Arrange(default);

            automaticGraphLayouter.DoLayoutNow(LayoutSettingsViewModel.CurrentAlgorithm!, addedQuests, addedConnections, false);

            //var maxRight = Elements.Select(x => x.Bounds.Right).DefaultIfEmpty().Max();
            var offset = targetLocation - new Point(mainQuests[^1]!.PerfectX, mainQuests[^1]!.PerfectY);

            foreach (var node in addedQuests)
            {
                node.PerfectX += offset.X;
                node.PerfectY += offset.Y;
                node.Location = new Point(node.PerfectX, node.PerfectY);
            }

            foreach (var q in questStore)
            {
                if (!existingData.ContainsKey(q.Entry))
                    newExistingData.Add(new ChainRawData(q.Template, q.OtherFactionQuest));
                if (!existingConditions.ContainsKey(q.Entry) && q.Conditions.Count > 0)
                    newExistingConditions.Add((q.Entry, q.Conditions));
            }

            if (nonFatalErrors.Count > 0 && !userPreferences.NeverShowIncorrectDatabaseDataWarning)
            {
                if (await messageBoxService.ShowDialog(
                    new MessageBoxFactory<bool>()
                        .SetTitle("Warning")
                        .SetMainInstruction("Incorrect database data")
                        .SetContent("Some data in the database is out of correct range: \n" +
                                    string.Join("\n", nonFatalErrors.Select(x => " - " + x)))
                        .WithOkButton(false)
                        .WithButton("(never show again)", true)
                        .Build()))
                {
                    userPreferences.NeverShowIncorrectDatabaseDataWarning = true;
                }
            }

            var wasSavedBeforeLoad = History.IsSaved;

            historyHandler.DoAction(new AnonymousHistoryAction("Load quest", () =>
            {
                //using var _ = Elements.SuspendNotifications();
                //using var __ = Connections.SuspendNotifications();
                foreach (var data in newExistingData)
                {
                    removedQuestsToReSave[data.Id] = data;
                    existingData.Remove(data.Id);
                }

                foreach (var (quest, data) in newExistingConditions)
                {
                    removedQuestConditionsToReSave[quest] = data;
                    existingConditions.Remove(quest);
                }

                foreach (var conn in addedConnections)
                    Connections.Remove(conn);

                foreach (var toAdd in addedQuests)
                {
                    if (toAdd is QuestViewModel questViewModel)
                        entryToQuest.Remove(questViewModel.Entry);

                    Elements.Remove(toAdd);
                }
            }, () =>
            {
                //using var _ = Elements.SuspendNotifications();
                //using var __ = Connections.SuspendNotifications();
                foreach (var toAdd in addedQuests)
                {
                    if (toAdd is ExclusiveGroupViewModel group)
                        Elements.Insert(0, group);
                    else if (toAdd is QuestViewModel qVm)
                    {
                        entryToQuest[qVm.Entry] = qVm;
                        Elements.Add(toAdd);
                    }
                    else
                        throw new InvalidOperationException("Unknown type of element to add");
                }
                foreach (var conn in addedConnections)
                {
                    Connections.Add(conn);
                }

                foreach (var data in newExistingData)
                {
                    existingData[data.Id] = data;
                    removedQuestsToReSave.Remove(data.Id);
                }

                foreach (var (quest, data) in newExistingConditions)
                {
                    existingConditions[quest] = data;
                    removedQuestConditionsToReSave.Remove(quest);
                }
            }));

            if (navigateToQuest && mainQuests.Count > 0)
            {
                NavigateToQuest?.Invoke(mainQuests[^1]);
                SelectedItems.Clear();
                SelectedItems.AddRange(mainQuests);
            }

            if (wasSavedBeforeLoad)
            {
                History.MarkAsSaved();
            }

            return mainQuests[0];
        }
        finally
        {
            LoadingStack--;
        }
    }

    public event Action<BaseQuestViewModel>? NavigateToQuest;

    private void RefreshProblems()
    {
        List<(IReadOnlyList<BaseQuestViewModel> affected, string explanation)> problemsOutput = new();
        VerifyImpossibleChains(problemsOutput);

        foreach (var old in problems.Value)
        {
            if (old is QuestChainInspectionResult result)
            {
                foreach (var affected in result.Affected)
                    affected.IsProblematic = false;
            }
        }

        if (problemsOutput.Count > 0)
        {
            problems.Value = problemsOutput.Select(x =>
                    new QuestChainInspectionResult(x.affected[0].EntryOrExclusiveGroupId, DiagnosticSeverity.Error,
                        x.explanation, x.affected))
                .ToList();
            foreach (var affected in problemsOutput.SelectMany(q => q.affected))
                affected.IsProblematic = true;
        }
        else
        {
            problems.Value = Array.Empty<IInspectionResult>();
        }
    }

    // based on prerequisites finds faction for the quest
    OtherFactionQuest.Hint FindFactionHintFor(BaseQuestViewModel vm)
    {
        OtherFactionQuest.Hint GetQuestHint(QuestViewModel qvm)
        {
            var isOnlyHorde = qvm.Races != 0 && (qvm.Races & CharacterRaces.AllAlliance) == 0;
            var isOnlyAlliance = qvm.Races != 0 && (qvm.Races & CharacterRaces.AllHorde) == 0;
            if (isOnlyHorde)
                return OtherFactionQuest.Hint.Horde;
            if (isOnlyAlliance)
                return OtherFactionQuest.Hint.Alliance;
            return OtherFactionQuest.Hint.None;
        }
        if (vm is QuestViewModel qvm && GetQuestHint(qvm) is {} hint && hint != OtherFactionQuest.Hint.None)
        {
            return hint;
        }
        else if (vm is ExclusiveGroupViewModel groupVm && groupVm.Quests.Count > 0)
        {
            OtherFactionQuest.Hint? hint_ = null;
            foreach (var quest in groupVm.Quests)
            {
                var questHint = FindFactionHintFor(quest);
                if (hint_ == null)
                {
                    hint_ = questHint;
                }
                else if (hint_ != questHint)
                {
                    hint_ = null;
                    break;
                }
            }
            if (hint_ != null && hint_ != OtherFactionQuest.Hint.None)
            {
                return hint_.Value;
            }
        }

        OtherFactionQuest.Hint? hint__ = null;
        var prerequisites = vm.Prerequisites;
        if (vm is QuestViewModel questViewModel && questViewModel.ExclusiveGroup is { } partOfGroup)
        {
            prerequisites = prerequisites.Concat(partOfGroup.Prerequisites);
        }
        foreach (var preReq in prerequisites)
        {
            if (preReq.requirementType == QuestRequirementType.Completed)
            {
                var preReqHint = FindFactionHintFor(preReq.prerequisite);
                if (hint__ == null)
                {
                    hint__ = preReqHint;
                }
                else if (hint__ != preReqHint)
                {
                    hint__ = null;
                    break;
                }
            }
        }
        return hint__ ?? OtherFactionQuest.Hint.None;
    }

    internal async Task<QuestStore> BuildCurrentQuestStore()
    {
        RefreshProblems();

        if (problems.Value.Count > 0)
        {
            throw new ImpossibleChainException(problems.Value[0].Message);
        }

        QuestStore store = new QuestStore();
        HashSet<ExclusiveGroupViewModel> processedGroups = new();
        foreach (var viewModel in Elements)
        {
            if (viewModel is QuestViewModel quest)
            {
                var model = await store.GetOrCreate(quest.Entry, questTemplateSource.GetTemplate);
                model.AllowableRaces = quest.Races;
                model.AllowableClasses = quest.Classes;
                model.OtherFactionQuest = quest.FactionChange is {} fc ? new OtherFactionQuest(fc.Item1.Entry, FindFactionHintFor(fc.quest)) : null;

                if (quest.HasConditions)
                    model.Conditions = quest.Conditions;

                foreach (var (prerequisite, requirementType, _) in quest.Prerequisites
                             .Concat(quest.ExclusiveGroup?.Prerequisites ?? Enumerable.Empty<(BaseQuestViewModel, QuestRequirementType, ConnectionViewModel)>()))
                {
                    if (prerequisite is QuestViewModel requiredQuest)
                    {
                        var group = requiredQuest.ExclusiveGroup;
                        var groupType = group?.GroupType ?? QuestGroupType.All;
                        var groupEntry = group?.EntryOrExclusiveGroupId ?? 0;
                        if (requirementType == QuestRequirementType.Breadcrumb) // breadcrumbs are never supposed to be grupped
                        {
                            groupType = QuestGroupType.All;
                            groupEntry = 0;
                        }
                        model.AddRequirement(requirementType, new QuestGroup(groupType, groupEntry, new[]{requiredQuest.Entry}));
                    }
                    else if (prerequisite is ExclusiveGroupViewModel requiredGroup)
                    {
                        processedGroups.Add(requiredGroup);
                        var groupId = (int)requiredGroup.Quests.Select(q => q.Entry).Min();
                        groupId *= requiredGroup.GroupType == QuestGroupType.All ? -1 : 1;
                        model.AddRequirement(requirementType, new QuestGroup(requiredGroup.GroupType, groupId, requiredGroup.Quests.Select(q => q.Entry).ToArray()));
                    }
                }
            }
        }

        // hanging groups, i.e. XOR group means only one quest can be accepted, yet the group is not connected to anything
        foreach (var viewModel in Elements)
        {
            if (viewModel is ExclusiveGroupViewModel group)
            {
                if (!processedGroups.Contains(group))
                {
                    store.AddAdditionalGroup(group.GroupType, group.Quests.Select(q => q.Entry).ToList());
                }
            }
        }

        return store;
    }
    
    public async Task<IQuery> GenerateQuery()
    {
        var store = await BuildCurrentQuestStore();
        var newConditions = store.Where(x => x.Conditions.Count > 0).ToDictionary(x => x.Entry, x => x.Conditions);
        var query = await chainGenerator.Generate(store, existingData);
        var sql = await queryGenerator.GenerateQuery(query.ToList(), existingData, newConditions, existingConditions);
        if (removedQuestsToReSave.Count == 0)
            return sql;

        var oldSave = await queryGenerator.GenerateQuery(removedQuestsToReSave.Values.ToList(), null, removedQuestConditionsToReSave, null);
        IMultiQuery transaction = Queries.BeginTransaction(DataDatabaseType.World);
        transaction.Add(sql);
        transaction.BlankLine();
        transaction.Comment("Restore values for quests that were loaded, then unloaded");
        transaction.Add(oldSave);
        return transaction.Close();
    }

    public void ScheduleReLayoutGraph()
    {
        if (pendingRelayout)
            return;

        pendingRelayout = true;

        mainThread.StartTimer(() =>
        {
            ReLayoutGraph(Elements, Connections);
            pendingRelayout = false;
            return false;
        }, TimeSpan.FromMilliseconds(1));
    }

    private void ReLayoutGraph(IReadOnlyList<BaseQuestViewModel> nodes, IReadOnlyList<ConnectionViewModel> connections)
    {
        if (!autoLayout)
            return;

        previousRelayout?.Cancel();
        previousRelayout = new CancellationTokenSource();

        async Task Calculate(CancellationToken token)
        {
            try
            {
                CalculatingGraphLayoutStack++;
                await automaticGraphLayouter.DoLayout(LayoutSettingsViewModel.CurrentAlgorithm!, nodes, connections, token);
            }
            finally
            {
                CalculatingGraphLayoutStack--;
            }
        }

        Calculate(previousRelayout.Token).ListenWarnings();
    }

    public bool Update()
    {
        if (!autoLayout)
            return true;

        foreach (var e in Elements)
            e.Force = new Vector2((float)e.X - (float)e.PerfectX, (float)e.Y - (float)e.PerfectY) * -1 * 1.1f;

        foreach (var e in Elements)
        {
            if (e.IsDragging)
                continue;

            if (e is QuestViewModel quest && quest.ExclusiveGroup != null)
                continue;

            e.Location += e.Force * 0.05f;
        }

        return true;
    }

    private bool isPickingQuest;

    private bool DoesNewEdgeIntroducesCycle(BaseQuestViewModel from, BaseQuestViewModel to, bool includingSiblings = false, bool bothWays = false)
    {
        if (IsElementInChildren(from, to) ||
            (bothWays && IsElementInChildren(to, from)))
        {
            messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                .SetTitle("Invalid connection")
                .SetMainInstruction("This connection introduces a cycle.")
                .SetContent("You can't have cycles in any quest chain.")
                .SetIcon(MessageBoxIcon.Warning)
                .WithOkButton(true)
                .Build()).ListenErrors();
            return true;
        }

        return false;
    }

    private List<ConnectionViewModel>? GetConflictingConnections(BaseQuestViewModel from, BaseQuestViewModel to,
        ConnectionType requirementType)
    {
        List<ConnectionViewModel>? conflicting = null;

        // if there is already a connection To -> From, then it conflicts with potential From -> To
        foreach (var (requirementFor, type, conn) in to.RequirementFor)
        {
            if (requirementFor == from)
            {
                conflicting ??= new();
                conflicting.Add(conn);
            }
        }

        // if there is already a connection From -> To, then it conflicts with potential new From -> To
        foreach (var (requirementFor, type, conn) in from.RequirementFor)
        {
            if (requirementFor == to)
            {
                conflicting ??= new();
                conflicting.Add(conn);
            }
        }

        if (requirementType == ConnectionType.FactionChange)
        {
             // if there is any faction change for either quest, then it conflicts with potential new From -> To
             if (from.FactionChange is { } existing)
             {
                 conflicting ??= new();
                conflicting.Add(existing.Item2);
             }
             if (to.FactionChange is { } existing2)
             {
                 conflicting ??= new();
                 conflicting.Add(existing2.Item2);
             }
        }

        return conflicting;
    }

    private void RemoveConflictingConnectionsOld(BaseQuestViewModel newChild, BaseQuestViewModel newParent)
    {
        var inputConnections = newChild.Prerequisites.ToList();
        for (var index = inputConnections.Count - 1; index >= 0; index--)
        {
            var existing = inputConnections[index];
            if (IsOnInputsPath(newParent, existing.prerequisite as QuestViewModel) || IsElementInChildren(newParent, existing.prerequisite))
            {
                existing.conn.Detach();
                Connections.Remove(existing.conn);
            }
        }
    }

    private bool IsElementInChildren(BaseQuestViewModel nodeToFind, BaseQuestViewModel? nodeToFindIn)
    {
        if (nodeToFindIn == null)
            return false;
        
        Queue<BaseQuestViewModel> leafs = new Queue<BaseQuestViewModel>();
        leafs.Enqueue(nodeToFindIn);

        while (leafs.Count > 0)
        {
            nodeToFindIn = leafs.Dequeue();
            if (nodeToFindIn == nodeToFind)
                return true;
            foreach (var (_, _, conn) in nodeToFindIn.RequirementFor)
            {
                if (conn.ToNode != null)
                    leafs.Enqueue((QuestViewModel)conn.ToNode);
            }
        }

        return false;
    }

    private bool IsOnInputsPath(BaseQuestViewModel nodeToFind, QuestViewModel? leaf)
    {
        if (leaf == null)
            return false;
        
        Queue<QuestViewModel> leafs = new Queue<QuestViewModel>();
        leafs.Enqueue(leaf);
        
        while (leafs.Count > 0)
        {
            leaf = leafs.Dequeue();
            if (leaf == nodeToFind)
                return true;
            foreach (var (parent, _, _) in leaf.Prerequisites)
            {
                leafs.Enqueue((QuestViewModel)parent);
            }
        }

        return false;
    }

    private void VerifyImpossibleChains(List<(IReadOnlyList<BaseQuestViewModel> affected, string explanation)> problems)
    {
        void AddProblem(string explanation, params BaseQuestViewModel[] affected)
        {
            problems.Add((affected.ToList(), explanation));
        }

        void DetectOverlappingExclusiveGroups()
        {
            foreach (var element in Elements)
            {
                if (element is QuestViewModel quest)
                {
                    if (quest.ExclusiveGroup != null)
                    {
                        foreach (var (requirement, reqType, _) in quest.RequirementFor
                                     .Where(x => x.requirementType is QuestRequirementType.Completed or QuestRequirementType.MustBeActive))
                        {
                            // all quests in the groups should have this connections, otherwise it's a problem

                            foreach (var q in quest.ExclusiveGroup.Quests)
                            {
                                if (!HasConnection(q, requirement, reqType))
                                {
                                    AddProblem($"Quest {quest} is a prerequisite to {requirement} but {q} is not even though they belong to the same group. It is impossible, if a quest belong to a group, all quests in the group become the prerequisites. Add missing connection or remove this connection.", quest, q);
                                }
                            }
                        }
                    }
                }
            }
        }

        // ALL groups that have only one quests are pointless
#pragma warning disable CS8321 // Local function is declared but never used
        void DetectHangingAllGroups()
#pragma warning restore CS8321 // Local function is declared but never used
        {
            foreach (var element in Elements)
            {
                if (element is ExclusiveGroupViewModel group)
                {
                    if (group.IsAllGroupType && group.Quests.Count == 1)
                    {
                        AddProblem("ALL group with only one quest is pointless. Remove the group.", group);
                    }
                }
            }
        }

        // each quest can have only one breadcrumb and it can't be a grup
        void OnlySingleBreadcrumbs()
        {
            foreach (var element in Elements)
            {
                if (element is QuestViewModel)
                {
                    bool alreadyHasBreadcrumb = false;
                    foreach (var (to, type, _) in element.RequirementFor)
                    {
                        if (type == QuestRequirementType.Breadcrumb)
                        {
                            if (alreadyHasBreadcrumb)
                            {
                                AddProblem($"Quest {element} has more than one breadcrumb (optional) connection, this is not possible.", element);
                                break;
                            }

                            if (to is ExclusiveGroupViewModel)
                            {
                                AddProblem("Breadcrumb (optional) connection can only lead to a quest, not to a group", element, to);
                                break;
                            }

                            alreadyHasBreadcrumb = true;
                        }
                    }
                }
                else if (element is ExclusiveGroupViewModel)
                {
                    foreach (var (to, type, _) in element.RequirementFor)
                    {
                        if (type == QuestRequirementType.Breadcrumb)
                        {
                            AddProblem("Exclusive groups can't have breadcrumb (optional) connections", element, to);
                            break;
                        }
                    }
                }
            }
        }

        void CheckFactionChange()
        {
            foreach (var quest in Elements
                         .Where(e => e is QuestViewModel)
                         .Cast<QuestViewModel>())
            {
                if (quest.OtherFactionQuest is not { } other)
                    continue;

                var thisQuestHint = Check(quest);
                var otherQuestHint = Check(other);
                if (thisQuestHint != OtherFactionQuest.Hint.None &&
                    otherQuestHint != OtherFactionQuest.Hint.None &&
                    thisQuestHint == otherQuestHint)
                {
                    AddProblem($"Quest {quest} has a faction change connection to {other}, but they have same faction requirements: {thisQuestHint} and {otherQuestHint}.", quest, other);
                }
            }
            OtherFactionQuest.Hint Check(QuestViewModel quest)
            {
                var hint = FindFactionHintFor(quest);
                if (hint == OtherFactionQuest.Hint.None)
                {
                    AddProblem($"Unable to determine faction requirement for quest {quest}, so it can't have a faction change connection", quest);
                }

                return hint;
            }
        }


        DetectOverlappingExclusiveGroups();
        //DetectHangingAllGroups(); // maybe they are not pointless? idk
        OnlySingleBreadcrumbs();
        CheckFactionChange();
    }

    private bool HasConnection(BaseQuestViewModel from, BaseQuestViewModel to, QuestRequirementType? expectedType = null)
    {
        return from.Connector.Connections.Any(x => x.FromNode == from && x.ToNode == to && (!expectedType.HasValue || x.RequirementType == expectedType.Value.ToConnectionType()));
    }

    public async Task<string?> PickPlayerName()
    {
        var players = await QuestChainsServerIntegrationService.GetPlayersAsync();
        if (players.Count == 0)
            throw new UserException("No logged players found");

        string? playerName;

        if (players.Count > 1)
            playerName = await itemFromListProvider.GetItemFromList(players.ToDictionary(x => x, x => new SelectOption(x)), false, "Select player");
        else
            playerName = players[0];

        return playerName;
    }

    public async Task EvaluateQuestStatus()
    {
        if (await PickPlayerName() is not { } playerName)
            return;

        var questIds = Elements.OfType<QuestViewModel>().Select(x => x.Entry).ToList();
        var questStates = await QuestChainsServerIntegrationService.GetQuestStatesAsync(playerName, questIds);

        foreach (var state in questStates)
        {
            if (!entryToQuest.TryGetValue(state.QuestId, out var vm))
                continue;

            vm.PlayerQuestStatus = state.Status;
            vm.PlayerCanStart = state.CanStart;
            vm.PlayerCanStartChecks = string.Join("\n", state.Checks.Where(x => !x.Item2).Select(x => $"{x.Item1}: {x.Item2}"));
        }

        hasQuestStatus = true;
    }

    public bool AutoLayout
    {
        get => autoLayout;
        set
        {
            SetProperty(ref autoLayout, value);
            userPreferences.AutoLayout = value;
            ReLayoutGraph(Elements, Connections);
        }
    }

    public bool HideFactionChangeArrows
    {
        get => hideFactionChangeArrows;
        set
        {
            SetProperty(ref hideFactionChangeArrows, value);
            userPreferences.HideFactionChangeArrows = value;
        }
    }

    private CancellationTokenSource? previousRelayout;

    [Notify] [AlsoNotify(nameof(IsCalculatingGraphLayout))] private int calculatingGraphLayoutStack;
    public bool IsCalculatingGraphLayout => calculatingGraphLayoutStack > 0;

    [Notify] [AlsoNotify(nameof(IsLoading))] private int loadingStack;
    public bool IsLoading => loadingStack > 0;

    public bool IsEmpty => Elements.Count == 0;

    private bool pendingRelayout;

    public ICommand DeleteSelectedCommand { get; }
    public ICommand Undo { get; set; }
    public ICommand Redo { get; set; }
    public IHistoryManager History { get; }
    public ICachedDatabaseProvider DatabaseProvider { get; }
    public bool IsModified => !History.IsSaved;
    public int DesiredWidth => 1024;
    public int DesiredHeight => 720;
    public string Title => "Quest chain";
    public bool Resizeable => true;
    public ICommand Copy { get; }
    public ICommand Cut => AlwaysDisabledCommand.Command;
    public ICommand Paste => AlwaysDisabledCommand.Command;
    public IAsyncCommand Save { get; }
    public IAsyncCommand? CloseCommand { get; set; }
    public bool CanClose => true;
    public ISolutionItem SolutionItem { get; set; }
    public bool RemoteCommandsSupported { get; }

    public QuestPickerViewModel QuestPicker { get; }
    public IQuestChainsServerIntegrationService QuestChainsServerIntegrationService { get; }
    public GraphLayoutSettingsViewModel LayoutSettingsViewModel { get; }

    private TaskCompletionSource<uint?>? questPickingTask;
    private bool autoLayout;
    private bool hideFactionChangeArrows;

    public bool IsPickingQuest
    {
        get => isPickingQuest;
        set
        {
            if (!value && questPickingTask != null)
            {
                questPickingTask.SetResult(null);
                questPickingTask = null;
            }
            SetProperty(ref isPickingQuest, value);
        }
    }

    public ICommand StartConnectionCommand { get; }
    public ICommand CreateConnectionCommand { get; }

    protected Task<uint?> PickQuest()
    {
        questPickingTask = new();
        QuestPicker.Reset();
        IsPickingQuest = true;
        return questPickingTask.Task;
    }

    private ReactiveProperty<IReadOnlyList<IInspectionResult>> problems = new(Array.Empty<IInspectionResult>());
    public IObservable<IReadOnlyList<IInspectionResult>> Problems => problems;

    public ImageUri? Icon => new ImageUri("Icons/document_quest_chain.png");

    public async Task LoadNewQuest(Point editorMouseLocation)
    {
        var quest = await PickQuest();
        if (quest.HasValue)
            await LoadQuestWithDependencies(quest.Value, editorMouseLocation, false);
    }

    public bool HasQuest(uint quest)
    {
        return entryToQuest.ContainsKey(quest);
    }

    public void ShowShiftKeyTeachingTip()
    {
        ShiftAltConnectionTeachingTipVisible = teachingTipService.ShowTip("QUEST_CHAIN_CONNECTION_FROM_QUEST");
    }
}