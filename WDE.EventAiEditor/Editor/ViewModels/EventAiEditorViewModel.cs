using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.Exporter;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor.UserControls;
using WDE.EventAiEditor.Editor.ViewModels.Editing;
using WDE.EventAiEditor.Exporter;
using WDE.EventAiEditor.Models;
using WDE.EventAiEditor.History;
using WDE.EventAiEditor.Inspections;
using WDE.SqlQueryGenerator;

namespace WDE.EventAiEditor.Editor.ViewModels
{
    public class EventAiEditorViewModel : ObservableBase, ISolutionItemDocument, IProblemSourceDocument, IDisposable
    {
        private readonly IDatabaseProvider database;
        private readonly IEventAiDatabaseProvider EventAiDatabase;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ITaskRunner taskRunner;
        private readonly IToolEventAiEditorViewModel eventAiEditorViewModel;
        private readonly IEventAiExporter EventAiExporter;
        private readonly IEventAiImporter EventAiImporter;
        private readonly IEditorFeatures editorFeatures;
        private readonly ITeachingTipService teachingTipService;
        private readonly IMainThread mainThread;
        private readonly IConditionEditService conditionEditService;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly IEventAiInspectorService inspectorService;
        private readonly IParameterPickerService parameterPickerService;
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IEventAiDataManager eventAiDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly IEventAiFactory eventAiFactory;
        private readonly IEventActionListProvider eventActionListProvider;
        private readonly IStatusBar statusbar;
        private readonly IWindowManager windowManager;
        private readonly IMessageBoxService messageBoxService;

        private IEventAiSolutionItem item;
        private EventAiScript script;

        public EventAiTeachingTips TeachingTips { get; private set; }

        public ImageUri? Icon { get; }

        private bool isLoading = true;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }

        public EventAiScript Script
        {
            get => script;
            set => SetProperty(ref script, value);
        }
        
        public EventAiEditorViewModel(IEventAiSolutionItem item,
            IHistoryManager history,
            IDatabaseProvider databaseProvider,
            IEventAiDatabaseProvider EventAiDatabase,
            IEventAggregator eventAggregator,
            IEventAiDataManager eventAiDataManager,
            IEventAiFactory eventAiFactory,
            IConditionDataManager conditionDataManager,
            IItemFromListProvider itemFromListProvider,
            IEventActionListProvider eventActionListProvider,
            IStatusBar statusbar,
            IWindowManager windowManager,
            IMessageBoxService messageBoxService,
            ISolutionItemNameRegistry itemNameRegistry,
            ITaskRunner taskRunner,
            IClipboardService clipboard,
            IToolEventAiEditorViewModel eventAiEditorViewModel,
            IEventAiExporter EventAiExporter,
            IEventAiImporter EventAiImporter,
            IEditorFeatures editorFeatures,
            ITeachingTipService teachingTipService,
            IMainThread mainThread,
            ISolutionItemIconRegistry iconRegistry,
            IConditionEditService conditionEditService,
            ICurrentCoreVersion currentCoreVersion,
            IEventAiInspectorService inspectorService,
            IParameterPickerService parameterPickerService,
            IMySqlExecutor mySqlExecutor)
        {
            History = history;
            this.database = databaseProvider;
            this.EventAiDatabase = EventAiDatabase;
            this.eventAiDataManager = eventAiDataManager;
            this.eventAiFactory = eventAiFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.eventActionListProvider = eventActionListProvider;
            this.statusbar = statusbar;
            this.windowManager = windowManager;
            this.messageBoxService = messageBoxService;
            this.itemNameRegistry = itemNameRegistry;
            this.taskRunner = taskRunner;
            this.eventAiEditorViewModel = eventAiEditorViewModel;
            this.EventAiExporter = EventAiExporter;
            this.EventAiImporter = EventAiImporter;
            this.editorFeatures = editorFeatures;
            this.teachingTipService = teachingTipService;
            this.mainThread = mainThread;
            this.conditionEditService = conditionEditService;
            this.currentCoreVersion = currentCoreVersion;
            this.inspectorService = inspectorService;
            this.parameterPickerService = parameterPickerService;
            this.mySqlExecutor = mySqlExecutor;
            this.conditionDataManager = conditionDataManager;
            script = null!;
            this.item = null!;
            TeachingTips = null!;
            title = "";
            Icon = iconRegistry.GetIcon(item);
            
            CloseCommand = new AsyncCommand(async () =>
            {
                if (eventAiEditorViewModel.CurrentScript == Script)
                    eventAiEditorViewModel.ShowEditor(null, null);
            });
            EditEvent = new DelegateCommand(EditEventCommand);
            DeselectAllButEvents = new DelegateCommand(() =>
            {
                foreach (EventAiEvent e in Events)
                {
                    if (!e.IsSelected)
                    {
                        foreach (var a in e.Actions)
                            a.IsSelected = false;
                    }
                }
            });
            DeselectAll = new DelegateCommand(() =>
            {
                foreach (EventAiEvent e in Events)
                {
                    foreach (var a in e.Actions)
                        a.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            DeselectAllButActions = new DelegateCommand(() =>
            {
                foreach (EventAiEvent e in Events)
                    e.IsSelected = false;
            });
            OnDropItems = new DelegateCommand<DropActionsConditionsArgs>(args =>
            {
                if (args == null)
                    return;

                var destIndex = args.EventIndex;
                
                using (script!.BulkEdit(args.Move ? "Reorder events" : "Copy events"))
                {
                    var selected = new List<EventAiEvent>();
                    int d = destIndex;
                    for (int i = Events.Count - 1; i >= 0; --i)
                    {
                        if (Events[i].IsSelected)
                        {
                            selected.Add(Events[i]);
                            Events[i].IsSelected = false;
                            if (args.Move)
                            {
                                if (i <= destIndex)
                                    d--;
                                script.Events.RemoveAt(i);
                            }
                        }
                    }

                    if (d == -1)
                        d = 0;
                    selected.Reverse();
                    foreach (EventAiEvent s in selected)
                    {
                        var newEvent = args.Copy ? s.DeepCopy() : s;
                        newEvent.IsSelected = true;
                        script.Events.Insert(d++, newEvent);
                    }
                }
            });
            OnDropActions = new DelegateCommand<DropActionsConditionsArgs>(data =>
            {
                using (script!.BulkEdit(data.Move ? "Reorder actions" : "Copy actions"))
                {
                    if (data.EventIndex < 0 || data.EventIndex >= Events.Count)
                    {
                        LOG.LogError("Fatal error! event index out of range");
                        return;
                    }
                    var selected = new List<EventAiAction>();
                    int d = data.ActionIndex;
                    for (var eventIndex = Events.Count - 1; eventIndex >= 0; eventIndex--)
                    {
                        EventAiEvent e = Events[eventIndex];
                        for (int i = e.Actions.Count - 1; i >= 0; --i)
                        {
                            if (e.Actions[i].IsSelected)
                            {
                                selected.Add(e.Actions[i]);
                                e.Actions[i].IsSelected = false;
                                if (data.Move)
                                {
                                    if (eventIndex == data.EventIndex && i < data.ActionIndex)
                                        d--;
                                    e.Actions.RemoveAt(i);
                                }
                            }
                        }
                    }

                    selected.Reverse();
                    foreach (EventAiAction s in selected)
                    {
                        EventAiAction newAction = data.Copy ? s.Copy() : s;
                        newAction.IsSelected = true;
                        Events[data.EventIndex].Actions.Insert(d++, newAction);
                    }
                }
            });
            
            EditAction = new AsyncAutoCommand<EventAiAction>(EditActionCommand);
            AddEvent = new AsyncAutoCommand(AddEventCommand);
            AddAction = new AsyncAutoCommand<NewActionViewModel>(AddActionCommand);

            
            DirectEditParameter = new DelegateCommand<object>(async obj =>
            {
                if (obj is ParameterWithContext param)
                {
                    (long? val, bool ok) = await parameterPickerService.PickParameter(param.Parameter.Parameter, param.Parameter.Value, param.Context);
                    if (ok)
                    {
                        param.Parameter.Value = val.Value;
                        if (param.Parameter.Parameter is ICustomPickerContextualParameter<long>) // custom pickers can save to database, which makes a delay when the value will be ready
                        {
                            param.Parameter.ForceRefresh();
                            mainThread.Delay(() => param.Context.InvalidateReadable(), TimeSpan.FromMilliseconds(50));
                        }
                    }
                } 
            });
            
            SaveCommand = new AsyncAutoCommand(() => 
                taskRunner.ScheduleTask("Save script to database", SaveAllToDb));

            DeleteAction = new DelegateCommand<EventAiAction>(DeleteActionCommand);
            DeleteSelected = new DelegateCommand(() =>
            {
                if (AnyEventSelected)
                {
                    using (script!.BulkEdit("Delete events"))
                    {
                        int? nextSelect = FirstSelectedIndex;
                        if (MultipleEventsSelected)
                            nextSelect = null;

                        for (int i = Events.Count - 1; i >= 0; --i)
                        {
                            if (Events[i].IsSelected)
                                Events.RemoveAt(i);
                        }

                        if (nextSelect.HasValue)
                        {
                            if (nextSelect.Value < Events.Count)
                                Events[nextSelect.Value].IsSelected = true;
                            else if (nextSelect.Value - 1 >= 0 && nextSelect.Value - 1 < Events.Count)
                                Events[nextSelect.Value - 1].IsSelected = true;
                        }
                    }
                }
                else if (AnyActionSelected)
                {
                    using (script!.BulkEdit("Delete actions"))
                    {
                        (int eventIndex, int actionIndex)? nextSelect = FirstSelectedActionIndex;
                        if (MultipleActionsSelected)
                            nextSelect = null;

                        for (var i = 0; i < Events.Count; ++i)
                        {
                            EventAiEvent e = Events[i];
                            for (int j = e.Actions.Count - 1; j >= 0; --j)
                            {
                                if (e.Actions[j].IsSelected)
                                    e.Actions.RemoveAt(j);
                            }
                        }

                        if (nextSelect.HasValue)
                        {
                            if (nextSelect.Value.actionIndex < Events[nextSelect.Value.eventIndex].Actions.Count)
                                Events[nextSelect.Value.eventIndex].Actions[nextSelect.Value.actionIndex].IsSelected = true;
                            else if (Events[nextSelect.Value.eventIndex].Actions.Count > 0 && 
                                     nextSelect.Value.actionIndex == Events[nextSelect.Value.eventIndex].Actions.Count)
                                Events[nextSelect.Value.eventIndex].Actions[nextSelect.Value.actionIndex - 1].IsSelected = true;
                        }
                    }
                }
            });

            UndoCommand = new DelegateCommand(history.Undo, () => history.CanUndo);
            RedoCommand = new DelegateCommand(history.Redo, () => history.CanRedo);

            EditSelected = new DelegateCommand(() =>
            {
                if (AnyEventSelected)
                {
                    if (!MultipleEventsSelected)
                        EditEventCommand();
                }
                else if (AnyActionSelected && !MultipleActionsSelected)
                    EditActionCommand(Events[FirstSelectedActionIndex.eventIndex].Actions[FirstSelectedActionIndex.actionIndex]).ListenErrors();
            });

            CopyCommand = new AsyncCommand(async () =>
            {
                var selectedEvents = Events.Where(e => e.IsSelected).ToList();
                if (selectedEvents.Count > 0)
                {
                    string eventLines = EventAiSerializer.SerializeEventsWithActions(selectedEvents);
                    clipboard.SetText(eventLines);
                }
                else if (AnyActionSelected)
                {
                    var selectedActions = Events.SelectMany(e => e.Actions).Where(e => e.IsSelected).ToList();
                    if (selectedActions.Count > 0)
                    {
                        var lines = EventAiSerializer.SerializeActions(selectedActions);
                        clipboard.SetText(lines);
                    }
                }
            });
            CutCommand = new AsyncCommand(async () =>
            {
                await CopyCommand.ExecuteAsync();
                DeleteSelected.Execute();
            });
            PasteCommand = new AsyncCommand(async () =>
            {
                if (string.IsNullOrEmpty(await clipboard.GetText()))
                    return;
                
                var content = await clipboard.GetText() ?? "";

                if (!content.StartsWith("@event") && !content.StartsWith("@action"))
                    return;
                
                var onlyActions = content.StartsWith("@action");
                
                var lines = content.Split('\n');
                
                if (onlyActions)
                {
                    int? eventIndex = null;
                    int? actionIndex = null;
                    using (script!.BulkEdit("Paste actions"))
                    {
                        for (var i = 0; i < Events.Count; ++i)
                        {
                            if (Events[i].IsSelected)
                                eventIndex = i;

                            for (int j = Events[i].Actions.Count - 1; j >= 0; j--)
                            {
                                if (Events[i].Actions[j].IsSelected)
                                {
                                    eventIndex = i;
                                    if (!actionIndex.HasValue)
                                        actionIndex = j + 1;
                                    //else
                                    //    actionIndex--;
                                    //Events[i].Actions.RemoveAt(j);
                                }
                            }
                        }

                        if (!eventIndex.HasValue)
                            eventIndex = Events.Count - 1;

                        if (eventIndex < 0)
                            return;

                        if (!actionIndex.HasValue)
                            actionIndex = Events[eventIndex.Value].Actions.Count - 1;

                        if (actionIndex < 0)
                            actionIndex = 0;

                        DeselectAll.Execute();
                        foreach (var line in lines)
                        {
                            if (EventAiSerializer.TryDeserializeAction(line, this.eventAiFactory, out var action))
                            {
                                Events[eventIndex.Value].Actions.Insert(actionIndex!.Value, action);
                                action.IsSelected = true;
                                actionIndex++;
                            }
                        }
                    }
                }
                else
                {
                    int? index = null;
                    using (script!.BulkEdit("Paste events"))
                    {
                        for (int i = Events.Count - 1; i >= 0; --i)
                        {
                            if (Events[i].IsSelected)
                            {
                                if (!index.HasValue)
                                    index = i + 1;
                                //else
                                //    index--;
                                //Events.RemoveAt(i);
                                Events[i].IsSelected = false;
                            }
                        }

                        if (!index.HasValue)
                            index = Events.Count;

                        EventAiEvent? lastEvent = null;
                        var j = index.Value;
                        foreach (var line in lines)
                        {
                            if (EventAiSerializer.TryDeserializeAction(line, this.eventAiFactory, out var action))
                            {
                                lastEvent?.Actions.Add(action);
                            }
                            else if (EventAiSerializer.TryDeserializeEvent(line, this.eventAiFactory, out var e))
                            {
                                lastEvent = e;
                                Events.Insert(j++, e);
                                e.IsSelected = true;
                            }
                        }
                    }
                }
                OnPaste?.Invoke();
            });

            Action<bool, int> selectionUpDown = (addToSelection, diff) =>
            {
                if (AnyEventSelected)
                {
                    int selectedEventIndex = Math.Clamp(FirstSelectedIndex + diff, 0, Events.Count - 1);
                    if (addToSelection)
                    {
                        Events[selectedEventIndex].IsSelected = true;
                    }
                    else
                    {
                        DeselectAll.Execute();
                        Events[selectedEventIndex].IsSelected = true;
                    }
                }
                else if (AnyActionSelected)
                {
                    int nextActionIndex = FirstSelectedActionIndex.actionIndex + diff;
                    int nextEventIndex = FirstSelectedActionIndex.eventIndex;
                    while (nextActionIndex == -1 || nextActionIndex >= Events[nextEventIndex].Actions.Count)
                    {
                        nextEventIndex += diff;
                        if (nextEventIndex >= 0 && nextEventIndex < Events.Count)
                        {
                            nextActionIndex = diff > 0
                                ? Events[nextEventIndex].Actions.Count > 0 ? 0 : -1
                                : Events[nextEventIndex].Actions.Count - 1;
                        }
                        else
                            break;
                    }

                    if (nextActionIndex != -1 && nextEventIndex >= 0 && nextEventIndex < Events.Count)
                    {
                        DeselectAll.Execute();
                        Events[nextEventIndex].Actions[nextActionIndex].IsSelected = true;
                    }
                }
                else
                {
                    if (Events.Count > 0)
                        Events[diff > 0 ? 0 : Events.Count - 1].IsSelected = true;
                }
            };

            SelectionUp = new DelegateCommand<bool?>(addToSelection => selectionUpDown(addToSelection ?? false, -1));
            SelectionDown = new DelegateCommand<bool?>(addToSelection => selectionUpDown(addToSelection ?? false, 1));
            SelectionLeft = new DelegateCommand(() =>
            {
                if (!AnyEventSelected && AnyActionSelected)
                {
                    (int eventIndex, int actionIndex) actionEventIndex = FirstSelectedActionIndex;
                    DeselectAll.Execute();
                    Events[actionEventIndex.eventIndex].IsSelected = true;
                }
                else if (!AnyEventSelected && !AnyActionSelected)
                    selectionUpDown(false, -1);
            });
            SelectionRight = new DelegateCommand(() =>
            {
                if (!AnyEventSelected)
                    selectionUpDown(false, -1);
                if (AnyEventSelected)
                {
                    int eventIndex = FirstSelectedIndex;
                    if (Events[eventIndex].Actions.Count > 0)
                    {
                        DeselectAll.Execute();
                        Events[eventIndex].Actions[0].IsSelected = true;
                    }
                }
            });

            SelectAll = new DelegateCommand(() =>
            {
                foreach (EventAiEvent e in Events)
                    e.IsSelected = true;
            });

            History.PropertyChanged += (sender, args) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };

            AutoDispose(problems.Subscribe(problems =>
            {
                var lines = problematicLines ?? new();
                lines.Clear();
                foreach (var p in problems)
                {
                    lines[p.Line] = p.Severity;
                }

                ProblematicLines = null;
                ProblematicLines = lines;
            }));

            AutoDispose(new ActionDisposable(() => disposed = true));
            
            SetSolutionItem(item);
        }

        public Task<IQuery> GenerateQuery()
        {
            var critical = inspectorService.GenerateInspections(script)
                .Where(c => c.Severity == DiagnosticSeverity.Critical)
                .ToList();
            if (critical.Count > 0)
                throw new Exception("Critical errors found:   " + string.Join("\n   ", critical.Select(c => c.Message)));
            return EventAiExporter.GenerateSql(item, script);
        }

        public string Name => itemNameRegistry.GetName(item);

        public ObservableCollection<EventAiEvent> Events => script.Events;

        public ObservableCollection<object> Together { get; } = new();

        public EventAiEvent? SelectedItem => Events.FirstOrDefault(ev => ev.IsSelected);

        public DelegateCommand EditEvent { get; set; }

        public DelegateCommand DeselectAll { get; set; }
        public DelegateCommand DeselectAllButActions { get; set; }
        public DelegateCommand DeselectAllButEvents { get; set; }

        public DelegateCommand<object> DirectEditParameter { get; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropActions { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropItems { get; set; }

        public AsyncAutoCommand<NewActionViewModel> AddAction { get; set; }

        public DelegateCommand<EventAiAction> DeleteAction { get; set; }
        public AsyncAutoCommand<EventAiAction> EditAction { get; set; }
        public AsyncAutoCommand AddEvent { get; set; }

        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand RedoCommand { get; set; }

        public AsyncCommand CopyCommand { get; set; }
        public AsyncCommand CutCommand { get; set; }
        public AsyncCommand PasteCommand { get; set; }
        public event Action? OnPaste;
        public IAsyncCommand SaveCommand { get; set; }
        public DelegateCommand DeleteSelected { get; set; }
        public DelegateCommand EditSelected { get; set; }

        public DelegateCommand<bool?> SelectionUp { get; set; }
        public DelegateCommand<bool?> SelectionDown { get; set; }
        public DelegateCommand SelectionRight { get; set; }
        public DelegateCommand SelectionLeft { get; set; }
        public DelegateCommand SelectAll { get; set; }

        private bool AnyEventSelected => Events.Any(e => e.IsSelected);
        private bool MultipleEventsSelected => Events.Count(e => e.IsSelected) >= 2;
        private bool MultipleActionsSelected => Events.SelectMany(e => e.Actions).Count(a => a.IsSelected) >= 2;
        private bool AnyActionSelected => Events.SelectMany(e => e.Actions).Any(a => a.IsSelected);
        
        private int FirstSelectedIndex =>
            Events.Select((e, index) => (e.IsSelected, index)).Where(p => p.IsSelected).Select(p => p.index).FirstOrDefault();

        private (int eventIndex, int actionIndex) FirstSelectedActionIndex =>
            Events.SelectMany((e, eventIndex) => e.Actions.Select((a, actionIndex) => (a.IsSelected, eventIndex, actionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.actionIndex))
                .FirstOrDefault();
                
        public bool CanClose { get; } = true;
        public IHistoryManager History { get; }

        private string title;
        public string Title
        {
            get => title;
            set => SetProperty(ref title, value);
        }

        public bool IsModified => !History.IsSaved;
        public ICommand Undo => UndoCommand;
        public ICommand Redo => RedoCommand;

        public ICommand Copy => CopyCommand;
        public ICommand Cut => CutCommand;
        public ICommand Paste => PasteCommand;
        public IAsyncCommand Save => SaveCommand;
        public IAsyncCommand? CloseCommand { get; set; }

        private bool disposed = false;
        
        private void SetSolutionItem(IEventAiSolutionItem item)
        {
            Debug.Assert(this.item == null);
            this.item = item;
            Title = itemNameRegistry.GetName(item);
            Script = new EventAiScript(this.item, eventAiFactory, eventAiDataManager, messageBoxService);

            bool updateInspections = false;
            new Thread(() =>
            {
                Stopwatch sw = new();
                while (!disposed)
                {
                    if (updateInspections)
                    {
                        mainThread.Dispatch(() =>
                        {
                            problems.Value = inspectorService.GenerateInspections(script);
                        });
                        updateInspections = false;
                    }
                    Thread.Sleep(1200);
                }
            }).Start();
            script.EventChanged += (e, a, mask) =>
            {
                updateInspections = true;
            };
            
            TeachingTips = AutoDispose(new EventAiTeachingTips(messageBoxService, teachingTipService, this, Script));
            Script.ScriptSelectedChanged += EventChildrenSelectionChanged;
            
            Together.Add(new NewActionViewModel());
            
            AutoDispose(script.AllObjectsFlat.ToStream(false).Subscribe((e) =>
            {
                if (e.Type == CollectionEventType.Add)
                    Together.Insert(Together.Count - 1, e.Item);
                else
                    Together.Remove(e.Item);
            }));
            
            taskRunner.ScheduleTask($"Loading script {Title}", AsyncLoad);
        }

        private EventAiBaseElement? currentlyPreviewedElement = null;
        private ParametersEditViewModel? currentlyPreviewedViewModel = null;

        private void EventChildrenSelectionChanged()
        {
            if (!eventAiEditorViewModel.IsOpened)
                return;
            
            EventAiBaseElement? newPreviewedElement = null;
                
            if (AnyEventSelected)
            {
                if (!MultipleEventsSelected)
                    newPreviewedElement = script.Events[FirstSelectedIndex];
            }

            else if (AnyActionSelected)
            {
                if (!MultipleActionsSelected)
                    newPreviewedElement = script.Events[FirstSelectedActionIndex.eventIndex]
                        .Actions[FirstSelectedActionIndex.actionIndex];
            }

            if (newPreviewedElement != currentlyPreviewedElement)
            {
                currentlyPreviewedViewModel?.Dispose();
                currentlyPreviewedViewModel = null;
                currentlyPreviewedElement = newPreviewedElement;

                if (currentlyPreviewedElement == null)
                {
                        
                }
                else if (currentlyPreviewedElement is EventAiEvent ee)
                    currentlyPreviewedViewModel = EventEditViewModel(ee, true);
                else if (currentlyPreviewedElement is EventAiAction aa)
                    currentlyPreviewedViewModel = ActionEditViewModel(aa, true);

                if (currentlyPreviewedViewModel != null)
                    currentlyPreviewedViewModel.ShowCloseButtons = false;
                
                eventAiEditorViewModel.ShowEditor(Script, currentlyPreviewedViewModel);
            }
        }

        private async Task AsyncLoad()
        {
            var lines = (await EventAiDatabase.GetScriptFor(item.EntryOrGuid)).ToList();
            await EventAiImporter.Import(script, false, lines);
            IsLoading = false;
            History.AddHandler(new EventAiHistoryHandler(script, eventAiFactory));
            TeachingTips.Start();
            problems.Value = inspectorService.GenerateInspections(script);
        }
        
        private async Task SaveAllToDb()
        {
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving to database"));

            try
            {
                var query = await GenerateQuery();
                await mySqlExecutor.ExecuteSql(query);

                statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));

                History.MarkAsSaved();
            }
            catch (Exception e)
            {
                statusbar.PublishNotification(new PlainNotification(NotificationType.Error, "Failed to save to database"));
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Error")
                    .SetMainInstruction("Couldn't save to database")
                    .SetContent(e.Message)
                    .WithOkButton(true).Build());
            }
        }

        private void DeleteActionCommand(EventAiAction obj)
        {
            obj.Parent?.Actions.Remove(obj);
        }

        private async Task AddActionCommand(NewActionViewModel obj)
        {
            EventAiEvent? e = obj.Event;
            if (e == null)
                return;
            
            uint? actionId = await ShowActionPicker(e);

            if (!actionId.HasValue)
                return;

            EventActionGenericJsonData actionData = eventAiDataManager.GetRawData(EventOrAction.Action, actionId.Value);

            EventAiAction ev = eventAiFactory.ActionFactory(actionId.Value);
            ev.Parent = e;
            bool anyUsed = ev.GetParameter(0).IsUsed || actionData.CommentField != null;
            if (!anyUsed || await EditActionCommand(ev))
                e.Actions.Add(ev);
        }
        
        private async Task<uint?> ShowEventPicker()
        {
            var result = await eventActionListProvider.Get(EventOrAction.Event, _ => true);
            if (result.HasValue)
                return result.Value.Item1;
            return null;
        }

        private async Task<uint?> ShowActionPicker(EventAiEvent? parentEvent, bool showCommentMetaAction = true)
        {
            var result = await eventActionListProvider.Get(EventOrAction.Action,
                data =>
                {
                    return (showCommentMetaAction || data.Id != EventAiConstants.ActionComment);
                });
            if (result.HasValue)
                return result.Value.Item1;
            return null;
        }

        private async Task AddEventCommand()
        {
            DeselectAll.Execute();
            uint? id = await ShowEventPicker();

            if (id.HasValue)
            {
                EventAiEvent ev = eventAiFactory.EventFactory(id.Value);
                ev.Parent = script;
                if (!ev.GetParameter(0).IsUsed || await EditEventCommand(ev))
                    script.Events.Add(ev);
            }
        }

        private ParametersEditViewModel ActionEditViewModel(EventAiAction originalAction, bool editOriginal = false)
        {
            EventAiAction obj = editOriginal ? originalAction : originalAction.Copy();
            if (!editOriginal)
                obj.Parent = originalAction.Parent; // fake parent
            EventActionGenericJsonData actionData = eventAiDataManager.GetRawData(EventOrAction.Action, originalAction.Id);
            
            var parametersList = new List<(ParameterValueHolder<long>, string)>();
            var floatParametersList = new List<(ParameterValueHolder<float>, string)>();
            var actionList = new List<EditableActionData>();
            var stringParametersList = new List<(ParameterValueHolder<string>, string)>() {(obj.CommentParameter, "Comment")};
            
            for (var i = 0; i < obj.ParametersCount; ++i)
                parametersList.Add((obj.GetParameter(i), "Action"));
            
            if (actionData.Id != EventAiConstants.ActionComment)
            {
                actionList.Add(new EditableActionData("Type", "Action", async () =>
                {
                    uint? newActionIndex = await ShowActionPicker(obj.Parent, false);
                    if (!newActionIndex.HasValue)
                        return;
                    
                    eventAiFactory.UpdateAction(obj, newActionIndex.Value);
                    
                }, obj.ToObservable(e => e.Id).Select(id => eventAiDataManager.GetRawData(EventOrAction.Action, id).NameReadable)));
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                currentCoreVersion,
                parameterPickerService,
                obj, 
                !editOriginal,
                parametersList, 
                floatParametersList, 
                stringParametersList, 
                actionList,
                () =>
                {
                    actionData = eventAiDataManager.GetRawData(EventOrAction.Action, obj.Id);
                    var actionName = "Edit action " + obj.Readable;
                    if (originalAction.Id == EventAiConstants.ActionComment)
                        actionName = "Edit comment";
                    using (originalAction.BulkEdit(actionName))
                    {
                        if (obj.Id != originalAction.Id)
                            eventAiFactory.UpdateAction(originalAction, obj.Id);
                    
                        for (var i = 0; i < originalAction.ParametersCount; ++i)
                            originalAction.GetParameter(i).Value = obj.GetParameter(i).Value;

                        originalAction.Comment = obj.Comment;
                    }
                }, context: obj, focusFirstGroup: "Action");
            return viewModel;
        }

        private async Task<bool> EditActionCommand(EventAiAction eventAiAction)
        {
            var viewModel = ActionEditViewModel(eventAiAction);
            var result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        private void EditEventCommand()
        {
            if (SelectedItem != null)
                EditEventCommand(SelectedItem).ListenErrors();
        }

        private ParametersEditViewModel EventEditViewModel(EventAiEvent originalEventAiEvent, bool editOriginal = false)
        {
            EventAiEvent ev = editOriginal ? originalEventAiEvent : originalEventAiEvent.ShallowCopy();
            if (!editOriginal)
                ev.Parent = originalEventAiEvent.Parent; // fake parent
            
            var actionList = new List<EditableActionData>();
            var parametersList = new List<(ParameterValueHolder<long>, string)>
            {
                (ev.Chance, "General"),
                (ev.Flags, "General"),
                (ev.Phases, "General")
            };

            actionList.Add(new EditableActionData("Event", "General", async () =>
            {
                uint? newEventIndex = await ShowEventPicker();
                if (!newEventIndex.HasValue)
                    return;

                eventAiFactory.UpdateEvent(ev, newEventIndex.Value);
            }, ev.ToObservable(e => e.Id).Select(id => eventAiDataManager.GetRawData(EventOrAction.Event, id).NameReadable)));
            
            for (var i = 0; i < ev.ParametersCount; ++i)
                parametersList.Add((ev.GetParameter(i), "Event specific"));

            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                currentCoreVersion,
                parameterPickerService,
                ev,
                !editOriginal,
                parametersList,
                null, 
                null, 
                actionList,
                () =>
                    {
                        using (originalEventAiEvent.BulkEdit("Edit event " + ev.Readable))
                        {
                            if (originalEventAiEvent.Id != ev.Id)
                                eventAiFactory.UpdateEvent(originalEventAiEvent, ev.Id);
                            
                            originalEventAiEvent.Chance.Value = ev.Chance.Value;
                            originalEventAiEvent.Flags.Value = ev.Flags.Value;
                            originalEventAiEvent.Phases.Value = ev.Phases.Value;
                            for (var i = 0; i < originalEventAiEvent.ParametersCount; ++i)
                                originalEventAiEvent.GetParameter(i).Value = ev.GetParameter(i).Value;
                        }
                    },
                "Event specific", context: ev);

            return viewModel;
        }
        
        private async Task<bool> EditEventCommand(EventAiEvent originalEventAiEvent)
        {
            var viewModel = EventEditViewModel(originalEventAiEvent);
            bool result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        public ISolutionItem SolutionItem => item;
        public IObservable<IReadOnlyList<IInspectionResult>> Problems => problems;
        private ReactiveProperty<IReadOnlyList<IInspectionResult>> problems = new(new List<IInspectionResult>());

        private Dictionary<int, DiagnosticSeverity>? problematicLines = new();
        public Dictionary<int, DiagnosticSeverity>? ProblematicLines
        {
            get => problematicLines;
            set => SetProperty(ref problematicLines, value);
        }
    }
}
