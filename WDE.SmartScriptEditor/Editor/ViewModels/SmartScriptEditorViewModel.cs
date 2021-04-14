using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.Exporter;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.History;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartScriptEditorViewModel : ObservableBase, ISolutionItemDocument, IDisposable
    {
        private readonly IDatabaseProvider database;
        private readonly ISmartScriptDatabaseProvider smartScriptDatabase;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ITaskRunner taskRunner;
        private readonly IToolSmartEditorViewModel smartEditorViewModel;
        private readonly ISmartScriptExporter smartScriptExporter;
        private readonly IEditorFeatures editorFeatures;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly ISmartFactory smartFactory;
        private readonly ISmartTypeListProvider smartTypeListProvider;
        private readonly IStatusBar statusbar;
        private readonly IWindowManager windowManager;
        private readonly IMessageBoxService messageBoxService;

        private ISmartScriptSolutionItem item;

        private SmartScript script;

        private bool isLoading = true;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }

        public SmartScript Script
        {
            get => script;
            set => SetProperty(ref script, value);
        }
        
        public SmartScriptEditorViewModel(ISmartScriptSolutionItem item,
            IHistoryManager history,
            IDatabaseProvider databaseProvider,
            ISmartScriptDatabaseProvider smartScriptDatabase,
            IEventAggregator eventAggregator,
            ISmartDataManager smartDataManager,
            ISmartFactory smartFactory,
            IConditionDataManager conditionDataManager,
            IItemFromListProvider itemFromListProvider,
            ISmartTypeListProvider smartTypeListProvider,
            IStatusBar statusbar,
            IWindowManager windowManager,
            IMessageBoxService messageBoxService,
            ISolutionItemNameRegistry itemNameRegistry,
            ITaskRunner taskRunner,
            IClipboardService clipboard,
            IToolSmartEditorViewModel smartEditorViewModel,
            ISmartScriptExporter smartScriptExporter,
            IEditorFeatures editorFeatures)
        {
            History = history;
            this.database = databaseProvider;
            this.smartScriptDatabase = smartScriptDatabase;
            this.smartDataManager = smartDataManager;
            this.smartFactory = smartFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.smartTypeListProvider = smartTypeListProvider;
            this.statusbar = statusbar;
            this.windowManager = windowManager;
            this.messageBoxService = messageBoxService;
            this.itemNameRegistry = itemNameRegistry;
            this.taskRunner = taskRunner;
            this.smartEditorViewModel = smartEditorViewModel;
            this.smartScriptExporter = smartScriptExporter;
            this.editorFeatures = editorFeatures;
            this.conditionDataManager = conditionDataManager;

            CloseCommand = new AsyncCommand(async () =>
            {
                if (smartEditorViewModel.CurrentScript == Script)
                    smartEditorViewModel.ShowEditor(null, null);
            });
            EditEvent = new DelegateCommand(EditEventCommand);
            DeselectActions = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    if (!e.IsSelected)
                    {
                        foreach (var a in e.Actions)
                            a.IsSelected = false;
                        foreach (var c in e.Conditions)
                            c.IsSelected = false;
                    }
                }
            });
            DeselectAll = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    foreach (var a in e.Actions)
                        a.IsSelected = false;
                    foreach (var c in e.Conditions)
                        c.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            DeselectAllButActions = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    foreach (var c in e.Conditions)
                        c.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            DeselectAllButConditions = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    foreach (var a in e.Actions)
                        a.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            OnDropItems = new DelegateCommand<int?>(destIndex =>
            {
                using (script.BulkEdit("Reorder events"))
                {
                    var selected = new List<SmartEvent>();
                    int d = destIndex.Value;
                    for (int i = Events.Count - 1; i >= 0; --i)
                    {
                        if (Events[i].IsSelected)
                        {
                            if (i <= destIndex)
                                d--;
                            selected.Add(Events[i]);
                            script.Events.RemoveAt(i);
                        }
                    }

                    if (d == -1)
                        d = 0;
                    selected.Reverse();
                    foreach (SmartEvent s in selected)
                        script.Events.Insert(d++, s);
                }
            });
            OnDropActions = new DelegateCommand<DropActionsConditionsArgs>(data =>
            {
                using (script.BulkEdit("Reorder actions"))
                {
                    var selected = new List<SmartAction>();
                    int d = data.ActionIndex;
                    for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                    {
                        SmartEvent e = Events[eventIndex];
                        for (int i = e.Actions.Count - 1; i >= 0; --i)
                        {
                            if (e.Actions[i].IsSelected)
                            {
                                if (eventIndex == data.EventIndex && i < data.ActionIndex)
                                    d--;
                                selected.Add(e.Actions[i]);
                                e.Actions.RemoveAt(i);
                            }
                        }
                    }

                    selected.Reverse();
                    foreach (SmartAction s in selected)
                        Events[data.EventIndex].Actions.Insert(d++, s);
                }
            });
            OnDropConditions = new DelegateCommand<DropActionsConditionsArgs>(data =>
            {
                using (script.BulkEdit("Reorder conditions"))
                {
                    var selected = new List<SmartCondition>();
                    int d = data.ActionIndex;
                    for (var eventIndex = 0; eventIndex < Events.Count; eventIndex++)
                    {
                        SmartEvent e = Events[eventIndex];
                        for (int i = e.Conditions.Count - 1; i >= 0; --i)
                        {
                            if (e.Conditions[i].IsSelected)
                            {
                                if (eventIndex == data.EventIndex && i < data.ActionIndex)
                                    d--;
                                selected.Add(e.Conditions[i]);
                                e.Conditions.RemoveAt(i);
                            }
                        }
                    }

                    selected.Reverse();
                    foreach (SmartCondition s in selected)
                        Events[data.EventIndex].Conditions.Insert(d++, s);
                }
            });
            
            EditAction = new AsyncAutoCommand<SmartAction>(EditActionCommand);
            EditCondition = new AsyncAutoCommand<SmartCondition>(EditConditionCommand);
            AddEvent = new AsyncAutoCommand(AddEventCommand);
            AddAction = new AsyncAutoCommand<NewActionViewModel>(AddActionCommand);
            AddCondition = new AsyncAutoCommand<NewConditionViewModel>(AddConditionCommand);

            DirectEditParameter = new DelegateCommand<object>(async obj =>
            {
                if (obj is ParameterValueHolder<long> param)
                {
                    long? val = await itemFromListProvider.GetItemFromList(param.Parameter.Items ?? new Dictionary<long, SelectOption>(), param.Parameter is FlagParameter, param.Value);
                    if (val.HasValue)
                        param.Value = val.Value;   
                } 
                else if (obj is MetaSmartSourceTargetEdit sourceTargetEdit)
                {
                    var actionData = smartDataManager.GetRawData(SmartType.SmartAction, sourceTargetEdit.RelatedAction.Id);
                    if (sourceTargetEdit.IsSource)
                    {
                        int? newSource = await ShowSourcePicker(sourceTargetEdit.RelatedAction.Parent, actionData);
                        if (newSource.HasValue)
                            smartFactory.UpdateSource(sourceTargetEdit.RelatedAction.Source, newSource.Value);                        
                    }
                    else
                    {
                        int? newTarget = await ShowTargetPicker(sourceTargetEdit.RelatedAction.Parent, actionData);
                        if (newTarget.HasValue)
                            smartFactory.UpdateTarget(sourceTargetEdit.RelatedAction.Target, newTarget.Value);   
                    }
                }
            });
            
            /*SaveCommand = new AsyncAutoCommand(SaveAllToDb,
                null,
                e =>
                {
                    statusbar.PublishNotification(new PlainNotification(NotificationType.Error,
                        "Error while saving script to the database: " + e.Message));
                });*/
            SaveCommand = new DelegateCommand(() =>
            {
                taskRunner.ScheduleTask("Save script to database", SaveAllToDb);
            });

            DeleteAction = new DelegateCommand<SmartAction>(DeleteActionCommand);
            DeleteSelected = new DelegateCommand(() =>
            {
                if (AnyEventSelected)
                {
                    using (script.BulkEdit("Delete events"))
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
                    using (script.BulkEdit("Delete actions"))
                    {
                        (int eventIndex, int actionIndex)? nextSelect = FirstSelectedActionIndex;
                        if (MultipleActionsSelected)
                            nextSelect = null;

                        for (var i = 0; i < Events.Count; ++i)
                        {
                            SmartEvent e = Events[i];
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
                else if (AnyConditionSelected)
                {
                    using (script.BulkEdit("Delete conditions"))
                    {
                        (int eventIndex, int conditionIndex)? nextSelect = FirstSelectedConditionIndex;
                        if (MultipleConditionsSelected)
                            nextSelect = null;

                        for (var i = 0; i < Events.Count; ++i)
                        {
                            SmartEvent e = Events[i];
                            for (int j = e.Conditions.Count - 1; j >= 0; --j)
                            {
                                if (e.Conditions[j].IsSelected)
                                    e.Conditions.RemoveAt(j);
                            }
                        }

                        if (nextSelect.HasValue)
                        {
                            if (nextSelect.Value.conditionIndex < Events[nextSelect.Value.eventIndex].Conditions.Count)
                                Events[nextSelect.Value.eventIndex].Conditions[nextSelect.Value.conditionIndex].IsSelected = true;
                            else if (Events[nextSelect.Value.eventIndex].Conditions.Count > 0 && 
                                     nextSelect.Value.conditionIndex == Events[nextSelect.Value.eventIndex].Conditions.Count)
                                Events[nextSelect.Value.eventIndex].Conditions[nextSelect.Value.conditionIndex - 1].IsSelected = true;
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
                    EditActionCommand(Events[FirstSelectedActionIndex.eventIndex].Actions[FirstSelectedActionIndex.actionIndex]);
                else if (AnyConditionSelected && !MultipleConditionsSelected)
                    EditConditionCommand(Events[FirstSelectedConditionIndex.eventIndex].Conditions[FirstSelectedConditionIndex.conditionIndex]);
            });

            CopyCommand = new DelegateCommand(() =>
            {
                var selectedEvents = Events.Where(e => e.IsSelected).ToList();
                if (selectedEvents.Count > 0)
                {
                    string eventLines = string.Join("\n",
                        selectedEvents.SelectMany((e, index) => e.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, index))
                            .Select(s => s.SerializeToString()));
                    string conditionLines = string.Join("\n",
                        selectedEvents.SelectMany((e, index) => e.ToConditionLines(SmartConstants.ConditionSourceSmartScript, script.EntryOrGuid, script.SourceType, index))
                            .Select(s => s.ToSqlString())); 
                    
                    if (string.IsNullOrEmpty(conditionLines))
                        clipboard.SetText(eventLines);
                    else
                        clipboard.SetText($"conditions:\n{conditionLines}\nevents:\n{eventLines}");
                }
                else if (AnyActionSelected)
                {
                    var selectedActions = Events.SelectMany(e => e.Actions).Where(e => e.IsSelected).ToList();
                    if (selectedActions.Count > 0)
                    {
                        SmartEvent fakeEvent = new(-1) {ReadableHint = "", Parent = script};
                        foreach (SmartAction a in selectedActions)
                            fakeEvent.AddAction(a.Copy());

                        string lines = string.Join("\n",
                            fakeEvent.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, 0).Select(s => s.SerializeToString()));
                        clipboard.SetText(lines);
                    }
                }
                else if (AnyConditionSelected)
                {
                    var selectedConditions = Events.SelectMany(e => e.Conditions).Where(e => e.IsSelected).ToList();
                    if (selectedConditions.Count > 0)
                    {
                        SmartEvent fakeEvent = new(-1) {ReadableHint = "", Parent = script};
                        foreach (SmartCondition c in selectedConditions)
                            fakeEvent.Conditions.Add(c.Copy());

                        string lines = string.Join("\n",
                            fakeEvent.ToConditionLines(SmartConstants.ConditionSourceSmartScript, script.EntryOrGuid, script.SourceType, 0).Select(s => s.ToSqlString()));
                        clipboard.SetText("conditions:\n" + lines);
                    }
                }
            });
            CutCommand = new DelegateCommand(() =>
            {
                CopyCommand.Execute();
                DeleteSelected.Execute();
            });
            PasteCommand = new DelegateCommand(async () =>
            {
                if (string.IsNullOrEmpty(await clipboard.GetText()))
                    return;
                
                var content = await clipboard.GetText() ?? "";
                List<IConditionLine> conditions = null;
                
                if (content.StartsWith("conditions:"))
                {
                    int eventsIndex = content.IndexOf("events:");
                    string conditionsString;
                    if (eventsIndex >= 0)
                    {
                        var eventsString = content.Substring(eventsIndex + 8).Trim();
                        conditionsString = content.Substring(12, eventsIndex - 12);
                        content = eventsString;
                    }
                    else 
                    {
                        conditionsString = content.Substring(12);
                        content = "";
                    }
                    
                    conditions = conditionsString.Split('\n')
                        .Select(line =>
                        {
                            if (line.TryToConditionLine(out IConditionLine s))
                                return s;
                            return null;
                        })
                        .Where(l => l != null)
                        .ToList();
                }
                
                var lines = content.Split('\n')
                    .Select(line =>
                    {
                        if (line.TryToISmartScriptLine(out ISmartScriptLine s))
                            return s;
                        return null;
                    })
                    .Where(l => l != null)
                    .ToList();
                if (lines.Count > 0)
                {
                    if (lines[0].EventType == -1) // actions
                    {
                        int? eventIndex = null;
                        int? actionIndex = null;
                        using (script.BulkEdit("Paste actions"))
                        {
                            for (var i = 0; i < Events.Count - 1; ++i)
                            {
                                if (Events[i].IsSelected)
                                    eventIndex = i;

                                for (int j = Events[i].Actions.Count - 1; j >= 0; j--)
                                {
                                    if (Events[i].Actions[j].IsSelected)
                                    {
                                        eventIndex = i;
                                        if (!actionIndex.HasValue)
                                            actionIndex = j;
                                        else
                                            actionIndex--;
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
                            foreach (var smartLine in lines)
                            {
                                var smartAction = script.SafeActionFactory(smartLine);
                                if (smartAction == null)
                                    continue;
                                smartAction.Comment = smartLine.Comment.Contains(" // ")
                                    ? smartLine.Comment.Substring(smartLine.Comment.IndexOf(" // ") + 4).Trim()
                                    : "";
                                Events[eventIndex.Value].Actions.Insert(actionIndex.Value, smartAction);
                                smartAction.IsSelected = true;
                                actionIndex++;
                            }
                        }
                    }
                    else
                    {
                        int? index = null;
                        using (script.BulkEdit("Paste events"))
                        {
                            for (int i = Events.Count - 1; i >= 0; --i)
                            {
                                if (Events[i].IsSelected)
                                {
                                    if (!index.HasValue)
                                        index = i;
                                    //else
                                    //    index--;
                                    //Events.RemoveAt(i);
                                    Events[i].IsSelected = false;
                                }
                            }

                            if (!index.HasValue)
                                index = Events.Count;
                            
                            foreach (var e in script.InsertFromClipboard(index.Value, lines, conditions))
                                e.IsSelected = true;
                        }
                    }
                }
                else if (conditions != null && conditions.Count > 0)
                {
                    if (AnyConditionSelected)
                    {
                        int? eventIndex = null;
                        int? conditionIndex = null;
                        using (script.BulkEdit("Paste conditions"))
                        {
                            for (var i = 0; i < Events.Count - 1; ++i)
                            {
                                if (Events[i].IsSelected)
                                    eventIndex = i;

                                for (int j = Events[i].Conditions.Count - 1; j >= 0; j--)
                                {
                                    if (Events[i].Conditions[j].IsSelected)
                                    {
                                        eventIndex = i;
                                        if (!conditionIndex.HasValue)
                                            conditionIndex = j;
                                        else
                                            conditionIndex--;
                                    }
                                }
                            }

                            if (!eventIndex.HasValue)
                                eventIndex = Events.Count - 1;

                            if (eventIndex < 0)
                                return;

                            if (!conditionIndex.HasValue)
                                conditionIndex = Events[eventIndex.Value].Conditions.Count - 1;

                            if (conditionIndex < 0)
                                conditionIndex = 0;

                            DeselectAll.Execute();
                            foreach (var smartCondition in conditions.Select(c => script.SafeConditionFactory(c)))
                            {
                                Events[eventIndex.Value].Conditions.Insert(conditionIndex.Value, smartCondition);
                                smartCondition.IsSelected = true;
                                conditionIndex++;
                            }
                        }      
                    }
                    else if (AnyEventSelected)
                    {
                        using (script.BulkEdit("Paste conditions"))
                        {
                            var smartEvent = SelectedItem;
                            DeselectAll.Execute();
                            foreach (var smartCondition in conditions.Select(c => script.SafeConditionFactory(c)))
                            {
                                smartEvent.Conditions.Add(smartCondition);
                                smartCondition.IsSelected = true;
                            }
                        }
                    }
                }
            });

            Action<bool, int> selectionUpDown = (addToSelection, diff) =>
            {
                if (AnyEventSelected)
                {
                    int selectedEventIndex = Math.Clamp(FirstSelectedIndex + diff, 0, Events.Count - 1);
                    if (!addToSelection)
                        DeselectAll.Execute();
                    Events[selectedEventIndex].IsSelected = true;
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
                else if (AnyConditionSelected)
                {
                    int nextConditionIndex = FirstSelectedConditionIndex.conditionIndex + diff;
                    int nextEventIndex = FirstSelectedConditionIndex.eventIndex;
                    while (nextConditionIndex == -1 || nextConditionIndex >= Events[nextEventIndex].Conditions.Count)
                    {
                        nextEventIndex += diff;
                        if (nextEventIndex >= 0 && nextEventIndex < Events.Count)
                        {
                            nextConditionIndex = diff > 0
                                ? Events[nextEventIndex].Conditions.Count > 0 ? 0 : -1
                                : Events[nextEventIndex].Conditions.Count - 1;
                        }
                        else
                            break;
                    }

                    if (nextConditionIndex != -1 && nextEventIndex >= 0 && nextEventIndex < Events.Count)
                    {
                        DeselectAll.Execute();
                        Events[nextEventIndex].Conditions[nextConditionIndex].IsSelected = true;
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
                foreach (SmartEvent e in Events)
                    e.IsSelected = true;
            });

            History.PropertyChanged += (sender, args) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };

            AutoDispose(eventAggregator.GetEvent<EventRequestGenerateSql>()
                .Subscribe(args =>
                {
                    if (args.Item is ISmartScriptSolutionItem)
                    {
                        ISmartScriptSolutionItem itemm = args.Item as ISmartScriptSolutionItem;
                        if (itemm.Entry == item.Entry && itemm.SmartType == item.SmartType)
                            args.Sql = smartScriptExporter.GenerateSql(script);
                    }
                }));

            SetSolutionItem(item);
        }

        public string Name => itemNameRegistry.GetName(item);

        public ObservableCollection<SmartEvent> Events => script.Events;

        public ObservableCollection<object> Together { get; } = new();

        public SmartEvent SelectedItem => Events.FirstOrDefault(ev => ev.IsSelected);

        public DelegateCommand EditEvent { get; set; }

        public DelegateCommand DeselectAll { get; set; }
        public DelegateCommand DeselectAllButConditions { get; set; }
        public DelegateCommand DeselectAllButActions { get; set; }
        public DelegateCommand DeselectActions { get; set; }

        public DelegateCommand<object> DirectEditParameter { get; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropConditions { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropActions { get; set; }
        public DelegateCommand<int?> OnDropItems { get; set; }

        public AsyncAutoCommand<NewActionViewModel> AddAction { get; set; }
        public AsyncAutoCommand<NewConditionViewModel> AddCondition { get; set; }

        public DelegateCommand<SmartAction> DeleteAction { get; set; }

        public AsyncAutoCommand<SmartAction> EditAction { get; set; }
        public AsyncAutoCommand<SmartCondition> EditCondition { get; 
        }
        public AsyncAutoCommand AddEvent { get; set; }

        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand RedoCommand { get; set; }

        public DelegateCommand CopyCommand { get; set; }
        public DelegateCommand CutCommand { get; set; }
        public DelegateCommand PasteCommand { get; set; }
        public DelegateCommand SaveCommand { get; set; }
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
        private bool MultipleConditionsSelected => Events.SelectMany(e => e.Conditions).Count(a => a.IsSelected) >= 2;
        private bool AnyConditionSelected => Events.SelectMany(e => e.Conditions).Any(a => a.IsSelected);

        private int FirstSelectedIndex =>
            Events.Select((e, index) => (e.IsSelected, index)).Where(p => p.IsSelected).Select(p => p.index).FirstOrDefault();

        private (int eventIndex, int actionIndex) FirstSelectedActionIndex =>
            Events.SelectMany((e, eventIndex) => e.Actions.Select((a, actionIndex) => (a.IsSelected, eventIndex, actionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.actionIndex))
                .FirstOrDefault();

        private (int eventIndex, int conditionIndex) FirstSelectedConditionIndex =>
            Events.SelectMany((e, eventIndex) => e.Conditions.Select((a, conditionIndex) => (a.IsSelected, eventIndex, conditionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.conditionIndex))
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
        public ICommand Save => SaveCommand;
        public AsyncAwaitBestPractices.MVVM.IAsyncCommand CloseCommand { get; set; }
        
        private void SetSolutionItem(ISmartScriptSolutionItem item)
        {
            Debug.Assert(this.item == null);
            this.item = item;
            Title = itemNameRegistry.GetName(item);
            
            Script = new SmartScript(this.item, smartFactory, smartDataManager, messageBoxService);
            Script.ScriptSelectedChanged += EventChildrenSelectionChanged;
            
            Together.Add(new NewActionViewModel());
            Together.Add(new NewConditionViewModel());
            
            AutoDispose(script.AllSmartObjectsFlat.ToStream().Subscribe((e) =>
            {
                if (e.Type == CollectionEventType.Add)
                    Together.Insert(Together.Count - 1, e.Item);
                else
                    Together.Remove(e.Item);
            }));
            
            taskRunner.ScheduleTask($"Loading script {Title}", AsyncLoad);
        }

        private SmartBaseElement currentlyPreviewedElement = null;
        private ParametersEditViewModel currentlyPreviewedViewModel = null;

        private void EventChildrenSelectionChanged()
        {
            if (!smartEditorViewModel.IsOpened)
                return;
            
            SmartBaseElement newPreviewedElement = null;
                
            if (AnyEventSelected)
            {
                if (!MultipleEventsSelected)
                    newPreviewedElement = script.Events[FirstSelectedIndex];
            }
            else if (AnyConditionSelected)
            {
                if (!MultipleConditionsSelected)
                    newPreviewedElement = script.Events[FirstSelectedConditionIndex.eventIndex]
                        .Conditions[FirstSelectedConditionIndex.conditionIndex];
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
                else if (currentlyPreviewedElement is SmartEvent ee)
                    currentlyPreviewedViewModel = EventEditViewModel(ee, true);
                else if (currentlyPreviewedElement is SmartAction aa)
                    currentlyPreviewedViewModel = ActionEditViewModel(aa, true);
                else if (currentlyPreviewedElement is SmartCondition cc)
                    currentlyPreviewedViewModel = ConditionEditViewModel(cc, true);

                if (currentlyPreviewedViewModel != null)
                    currentlyPreviewedViewModel.ShowCloseButtons = false;
                
                smartEditorViewModel.ShowEditor(Script, currentlyPreviewedViewModel);
            }
        }

        private async Task AsyncLoad()
        {
            var lines = smartScriptDatabase.GetScriptFor(item.Entry, item.SmartType).ToList();
            var conditions = smartScriptDatabase.GetConditionsForScript(item.Entry, item.SmartType).ToList();
            script.Load(lines, conditions);
            IsLoading = false;
            History.AddHandler(new SaiHistoryHandler(script, smartFactory));
        }
        
        private async Task SaveAllToDb()
        {
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving to database"));

            var (lines, conditions) = smartScriptExporter.ToDatabaseCompatibleSmartScript(script);
            
            await smartScriptDatabase.InstallScriptFor(item.Entry, item.SmartType, lines);

            await smartScriptDatabase.InstallConditionsForScript(conditions,
                item.Entry, item.SmartType);
            
            statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));
            
            History.MarkAsSaved();
        }

        private void DeleteActionCommand(SmartAction obj)
        {
            obj.Parent.Actions.Remove(obj);
        }

        private async Task AddActionCommand(NewActionViewModel obj)
        {
            SmartEvent e = obj.Event;
            if (e == null)
                return;
            int? sourceId = await ShowSourcePicker(e);

            if (!sourceId.HasValue)
                return;

            int? actionId = await ShowActionPicker(e, sourceId.Value);

            if (!actionId.HasValue)
                return;

            SmartGenericJsonData actionData = smartDataManager.GetRawData(SmartType.SmartAction, actionId.Value);

            SmartTarget target = null;

            if (!actionData.TargetIsSource && (actionData.TargetTypes?.Count ?? 0) > 0)
            {
                int? targetId = await ShowTargetPicker(e, actionData);

                if (!targetId.HasValue)
                    return;

                target = smartFactory.TargetFactory(targetId.Value);
            }
            else 
                target = smartFactory.TargetFactory(0);
            
            SmartSource source = smartFactory.SourceFactory(sourceId.Value);

            SmartAction ev = smartFactory.ActionFactory(actionId.Value, source, target);
            ev.Parent = e;
            if (await EditActionCommand(ev))
                e.Actions.Add(ev);
        }

        private async Task AddConditionCommand(NewConditionViewModel obj)
        {
            SmartEvent e = obj.Event;
            if (e == null)
                return;

            int? conditionId = await ShowConditionPicker();
            
            if (!conditionId.HasValue)
                return;

            var conditionData = conditionDataManager.GetConditionData(conditionId.Value);

            SmartCondition ev = smartFactory.ConditionFactory(conditionId.Value);
            ev.Parent = e;
            
            if ((conditionData.Parameters?.Count ?? 0) == 0 || await EditConditionCommand(ev))
                e.Conditions.Add(ev);
        }

        private Task<int?> ShowConditionPicker()
        {
            return smartTypeListProvider.Get(SmartType.SmartCondition, _ => true);
        }
        
        private Task<int?> ShowEventPicker()
        {
            return smartTypeListProvider.Get(SmartType.SmartEvent,
                data => data.UsableWithScriptTypes == null || data.UsableWithScriptTypes.Contains(script.SourceType));
        }

        private Task<int?> ShowTargetPicker(SmartEvent parentEvent, SmartGenericJsonData actionData)
        {           
            var eventSupportsActionInvoker =
                parentEvent?.Parent == null || parentEvent.Parent.GetEventData(parentEvent).Invoker != null;
            return smartTypeListProvider.Get(SmartType.SmartTarget,
                data =>
                {
                    if (data.UsableWithEventTypes != null && parentEvent != null && data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return (eventSupportsActionInvoker || !data.IsInvoker) &&
                           (data.UsableWithScriptTypes == null ||
                            data.UsableWithScriptTypes.Contains(script.SourceType)) &&
                           (actionData.TargetTypes == null || actionData.TargetTypes.Intersect(data.Types).Any());
                });
        }

        private Task<int?> ShowSourcePicker(SmartEvent parentEvent, SmartGenericJsonData? actionData = null)
        {
            var eventSupportsActionInvoker =
                parentEvent?.Parent == null || parentEvent.Parent.GetEventData(parentEvent).Invoker != null;
            return smartTypeListProvider.Get(SmartType.SmartSource,
                data =>
                {
                    if (!eventSupportsActionInvoker && data.IsInvoker)
                        return false;
                    
                    if (data.IsOnlyTarget)
                        return false;

                    if (actionData.HasValue && !IsSourceCompatibleWithAction(data.Id, actionData.Value))
                        return false;

                    if (data.UsableWithEventTypes != null && parentEvent != null && !data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return data.UsableWithScriptTypes == null || data.UsableWithScriptTypes.Contains(script.SourceType);
                });
        }

        private bool IsSourceCompatibleWithAction(int sourceId, SmartGenericJsonData actionData)
        {
            if (actionData.ImplicitSource != null)
            {
                var actionImplicitSource = smartDataManager.GetDataByName(SmartType.SmartTarget, actionData.ImplicitSource).Id;

                if (sourceId == actionImplicitSource)
                    return true;

                // kinda hack to show actions with NONE source with user pick SELF source
                // because it is natural for users to use SELF source for those actions
                return actionImplicitSource == SmartConstants.SourceNone && sourceId == SmartConstants.SourceSelf;
            }
            else
            {
                var sourceData = smartDataManager.GetRawData(SmartType.SmartSource, sourceId);

                IList<string> possibleSourcesOfAction = actionData.TargetIsSource ? actionData.TargetTypes : actionData.Sources;
                var possibleSourcesOfSource = sourceData.Types;

                if (possibleSourcesOfAction == null || possibleSourcesOfSource == null)
                    return false;

                return possibleSourcesOfAction.Intersect(possibleSourcesOfSource).Any();
            }
        }
        
        private Task<int?> ShowActionPicker(SmartEvent parentEvent, int sourceId, bool showCommentMetaAction = true)
        {
            return smartTypeListProvider.Get(SmartType.SmartAction,
                data =>
                {
                    if (data.UsableWithEventTypes != null && parentEvent != null && data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return (data.UsableWithScriptTypes == null ||
                            data.UsableWithScriptTypes.Contains(script.SourceType)) &&
                           IsSourceCompatibleWithAction(sourceId, data) &&
                           (showCommentMetaAction || data.Id != SmartConstants.ActionComment);
                });
        }

        private async Task AddEventCommand()
        {
            DeselectAll.Execute();
            int? id = await ShowEventPicker();

            if (id.HasValue)
            {
                SmartEvent ev = smartFactory.EventFactory(id.Value);
                ev.Parent = script;
                if (await EditEventCommand(ev))
                    script.Events.Add(ev);
            }
        }

        private ParametersEditViewModel ActionEditViewModel(SmartAction originalAction, bool editOriginal = false)
        {
            SmartAction obj = editOriginal ? originalAction : originalAction.Copy();
            if (!editOriginal)
                obj.Parent = originalAction.Parent; // fake parent
            SmartGenericJsonData actionData = smartDataManager.GetRawData(SmartType.SmartAction, originalAction.Id);
            
            var parametersList = new List<(ParameterValueHolder<long>, string)>();
            var floatParametersList = new List<(ParameterValueHolder<float>, string)>();
            var actionList = new List<EditableActionData>();
            var stringParametersList = new List<(ParameterValueHolder<string>, string)>() {(obj.CommentParameter, "Comment")};
            
            for (var i = 0; i < obj.Source.ParametersCount; ++i)
                parametersList.Add((obj.Source.GetParameter(i), "Source"));

            for (var i = 0; i < obj.ParametersCount; ++i)
                parametersList.Add((obj.GetParameter(i), "Action"));

            for (var i = 0; i < obj.Target.ParametersCount; ++i)
                parametersList.Add((obj.Target.GetParameter(i), "Target"));

            for (var i = 0; i < 4; ++i)
                floatParametersList.Add((obj.Target.Position[i], "Target"));

            var canPickTarget = obj.ToObservable(e => e.Id)
                .Select(id => smartDataManager.GetRawData(SmartType.SmartAction, id)).Select(actionData => !actionData.TargetIsSource && (actionData.TargetTypes?.Count ?? 0) > 0);

            if (actionData.Id != SmartConstants.ActionComment)
            {
                actionList.Add(new EditableActionData("Type", "Source", async () =>
                {
                    int? newSourceIndex = await ShowSourcePicker(obj.Parent);
                    if (!newSourceIndex.HasValue)
                        return;

                    var actionData = smartDataManager.GetRawData(SmartType.SmartAction, obj.Id);
                    if (!IsSourceCompatibleWithAction(newSourceIndex.Value, actionData))
                    {
                        var sourceData = smartDataManager.GetRawData(SmartType.SmartSource, newSourceIndex.Value);
                        var dialog = new MessageBoxFactory<bool>()
                            .SetTitle("Incorrect source for chosen action")
                            .SetMainInstruction($"The source you have chosen ({sourceData.NameReadable}) is not supported with action {actionData.NameReadable}");
                        if (string.IsNullOrEmpty(actionData.ImplicitSource))
                            dialog.SetContent($"Selected source can be one of: {string.Join(", ", sourceData.Types)}. However, current action requires one of: {string.Join(", ", actionData.TargetTypes)}");
                        else
                            dialog.SetContent($"In TrinityCore some actions do not support some sources, this is one of the case. Following action will ignore chosen source and will use source: {actionData.ImplicitSource}");
                        messageBoxService.ShowDialog(dialog.SetIcon(MessageBoxIcon.Information).Build());
                    }
                    
                    smartFactory.UpdateSource(obj.Source, newSourceIndex.Value);
                }, obj.Source.ToObservable(e => e.Id).Select(id => smartDataManager.GetRawData(SmartType.SmartSource, id).NameReadable)));

                actionList.Add(new EditableActionData("Type", "Action", async () =>
                {
                    int? newActionIndex = await ShowActionPicker(obj.Parent, obj.Source.Id, false);
                    if (!newActionIndex.HasValue)
                        return;

                    smartFactory.UpdateAction(obj, newActionIndex.Value);
                }, obj.ToObservable(e => e.Id).Select(id => smartDataManager.GetRawData(SmartType.SmartAction, id).NameReadable)));
            
                actionList.Add(new EditableActionData("Type", "Target", async () =>
                {
                    int? newTargetIndex = await ShowTargetPicker(obj.Parent, smartDataManager.GetRawData(SmartType.SmartAction, obj.Id));
                    if (!newTargetIndex.HasValue)
                        return;

                    smartFactory.UpdateTarget(obj.Target, newTargetIndex.Value);
                },  obj.Target.ToObservable(e => e.Id).Select(id => smartDataManager.GetRawData(SmartType.SmartTarget, id).NameReadable), canPickTarget.Not()));   
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                obj, 
                !editOriginal,
                parametersList, 
                floatParametersList, 
                stringParametersList, 
                actionList,
                () =>
                {
                    actionData = smartDataManager.GetRawData(SmartType.SmartAction, obj.Id);
                    var actionName = "Edit action " + obj.Readable;
                    if (originalAction.Id == SmartConstants.ActionComment)
                        actionName = "Edit comment";
                    using (originalAction.BulkEdit(actionName))
                    {
                        if (actionData.ImplicitSource != null)
                            smartFactory.UpdateSource(originalAction.Source, smartDataManager.GetDataByName(SmartType.SmartSource, actionData.ImplicitSource).Id);
                        else if (obj.Source.Id != originalAction.Source.Id)
                            smartFactory.UpdateSource(originalAction.Source, obj.Source.Id);
                    
                        if (obj.Target.Id != originalAction.Target.Id)
                            smartFactory.UpdateTarget(originalAction.Target, obj.Target.Id);

                        if (obj.Id != originalAction.Id)
                            smartFactory.UpdateAction(originalAction, obj.Id);
                    
                        for (var i = 0; i < originalAction.Target.Position.Length; ++i)
                            originalAction.Target.Position[i].Value = obj.Target.Position[i].Value;

                        for (var i = 0; i < originalAction.Target.ParametersCount; ++i)
                            originalAction.Target.GetParameter(i).Value = obj.Target.GetParameter(i).Value;

                        for (var i = 0; i < originalAction.Source.ParametersCount; ++i)
                            originalAction.Source.GetParameter(i).Value = obj.Source.GetParameter(i).Value;

                        for (var i = 0; i < originalAction.ParametersCount; ++i)
                            originalAction.GetParameter(i).Value = obj.GetParameter(i).Value;

                        originalAction.Comment = obj.Comment;
                    }
                });
            return viewModel;
        }

        private async Task<bool> EditActionCommand(SmartAction smartAction)
        {
            var viewModel = ActionEditViewModel(smartAction);
            var result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        private ParametersEditViewModel ConditionEditViewModel(SmartCondition originalCondition, bool editOriginal = false)
        {
            SmartCondition obj = editOriginal? originalCondition : originalCondition.Copy();
            if (!editOriginal)
                obj.Parent = originalCondition.Parent; // fake parent
            
            var actionList = new List<EditableActionData>();
            var parametersList = new List<(ParameterValueHolder<long>, string)>();

            parametersList.Add((obj.Inverted, "General"));
            parametersList.Add((obj.ConditionTarget, "General"));
            
            for (var i = 0; i < obj.ParametersCount; ++i)
            {                    
                parametersList.Add((obj.GetParameter(i), "Condition"));
            }

            actionList.Add(new EditableActionData("Condition", "General", async () =>
            {
                int? newConditionId = await ShowConditionPicker();
                if (!newConditionId.HasValue)
                    return;

                smartFactory.UpdateCondition(obj, newConditionId.Value);
            }, obj.ToObservable(e => e.Id).Select(id => conditionDataManager.GetConditionData(id).NameReadable)));
            
            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                obj, 
                !editOriginal,
                parametersList, 
                null, 
                null, 
                actionList,
                () =>
                {
                    using (originalCondition.BulkEdit("Edit condition " + obj.Readable))
                    {
                        if (obj.Id != originalCondition.Id)
                            smartFactory.UpdateCondition(originalCondition, obj.Id);
                    
                        originalCondition.Inverted.Value = obj.Inverted.Value;
                        originalCondition.ConditionTarget.Value = obj.ConditionTarget.Value;
                        for (var i = 0; i < originalCondition.ParametersCount; ++i)
                            originalCondition.GetParameter(i).Value = obj.GetParameter(i).Value;
                    }
                });
            
            return viewModel;
        }

        private async Task<bool> EditConditionCommand(SmartCondition originalCondition)
        {
            var viewModel = ConditionEditViewModel(originalCondition);
            bool result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        private void EditEventCommand()
        {
            if (SelectedItem != null)
                EditEventCommand(SelectedItem);
        }

        private ParametersEditViewModel EventEditViewModel(SmartEvent originalEvent, bool editOriginal = false)
        {
            SmartEvent ev = editOriginal ? originalEvent : originalEvent.ShallowCopy();
            if (!editOriginal)
                ev.Parent = originalEvent.Parent; // fake parent
            
            var actionList = new List<EditableActionData>();
            var parametersList = new List<(ParameterValueHolder<long>, string)>
            {
                (ev.Chance, "General"),
                (ev.Flags, "General"),
                (ev.Phases, "General")
            };
            if (editorFeatures.SupportsEventCooldown)
            {
                parametersList.Add((ev.CooldownMax, "General"));
                parametersList.Add((ev.CooldownMin, "General"));
            }

            actionList.Add(new EditableActionData("Event", "General", async () =>
            {
                int? newEventIndex = await ShowEventPicker();
                if (!newEventIndex.HasValue)
                    return;

                smartFactory.UpdateEvent(ev, newEventIndex.Value);
            }, ev.ToObservable(e => e.Id).Select(id => smartDataManager.GetRawData(SmartType.SmartEvent, id).NameReadable)));
            
            for (var i = 0; i < ev.ParametersCount; ++i)
                parametersList.Add((ev.GetParameter(i), "Event specific"));

            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                ev,
                !editOriginal,
                parametersList,
                null, 
                null, 
                actionList,
                () =>
                    {
                        using (originalEvent.BulkEdit("Edit event " + ev.Readable))
                        {
                            if (originalEvent.Id != ev.Id)
                                smartFactory.UpdateEvent(originalEvent, ev.Id);
                            
                            originalEvent.Chance.Value = ev.Chance.Value;
                            originalEvent.Flags.Value = ev.Flags.Value;
                            originalEvent.Phases.Value = ev.Phases.Value;
                            originalEvent.CooldownMax.Value = ev.CooldownMax.Value;
                            originalEvent.CooldownMin.Value = ev.CooldownMin.Value;
                            for (var i = 0; i < originalEvent.ParametersCount; ++i)
                                originalEvent.GetParameter(i).Value = ev.GetParameter(i).Value;
                        }
                    });

            return viewModel;
        }
        
        private async Task<bool> EditEventCommand(SmartEvent originalEvent)
        {
            var viewModel = EventEditViewModel(originalEvent);
            bool result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        public ISolutionItem SolutionItem => item;
    }
}