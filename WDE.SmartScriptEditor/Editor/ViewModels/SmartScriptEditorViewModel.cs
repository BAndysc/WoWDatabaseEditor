using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.Exporter;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.Views;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartScriptEditorViewModel : BindableBase, IDocument, IDisposable
    {
        private readonly IDatabaseProvider database;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ITaskRunner taskRunner;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly ISmartFactory smartFactory;
        private readonly ISmartTypeListProvider smartTypeListProvider;
        private readonly IStatusBar statusbar;
        private readonly IWindowManager windowManager;
        private readonly SubscriptionToken token;

        private SmartScriptSolutionItem item;

        private SmartScript script;

        private bool isLoading = true;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }
        
        public SmartScriptEditorViewModel(IHistoryManager history,
            IDatabaseProvider database,
            IEventAggregator eventAggregator,
            ISmartDataManager smartDataManager,
            ISmartFactory smartFactory,
            IConditionDataManager conditionDataManager,
            IItemFromListProvider itemFromListProvider,
            ISmartTypeListProvider smartTypeListProvider,
            IStatusBar statusbar,
            IWindowManager windowManager,
            ISolutionItemNameRegistry itemNameRegistry,
            ITaskRunner taskRunner)
        {
            History = history;
            this.database = database;
            this.smartDataManager = smartDataManager;
            this.smartFactory = smartFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.smartTypeListProvider = smartTypeListProvider;
            this.statusbar = statusbar;
            this.windowManager = windowManager;
            this.itemNameRegistry = itemNameRegistry;
            this.taskRunner = taskRunner;
            this.conditionDataManager = conditionDataManager;

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
            
            EditAction = new DelegateCommand<SmartAction>(a => EditActionCommand(a));
            EditCondition = new DelegateCommand<SmartCondition>(c => EditConditionCommand(c));
            AddEvent = new DelegateCommand(AddEventCommand);
            AddAction = new DelegateCommand<NewActionViewModel>(AddActionCommand);
            AddCondition = new DelegateCommand<NewConditionViewModel>(AddConditionCommand);

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
                            .Select(s => s.ToSqlString()));
                    string conditionLines = string.Join("\n",
                        selectedEvents.SelectMany((e, index) => e.ToConditionLines(script.EntryOrGuid, script.SourceType, index))
                            .Select(s => s.ToSqlString())); 
                    
                    if (string.IsNullOrEmpty(conditionLines))
                        Clipboard.SetText(eventLines);
                    else
                        Clipboard.SetText($"conditions:\n{conditionLines}\nevents:\n{eventLines}");
                }
                else if (AnyActionSelected)
                {
                    var selectedActions = Events.SelectMany(e => e.Actions).Where(e => e.IsSelected).ToList();
                    if (selectedActions.Count > 0)
                    {
                        SmartEvent fakeEvent = new(-1) {ReadableHint = ""};
                        foreach (SmartAction a in selectedActions)
                            fakeEvent.AddAction(a.Copy());

                        string lines = string.Join("\n",
                            fakeEvent.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, 0).Select(s => s.ToSqlString()));
                        Clipboard.SetText(lines);
                    }
                }
                else if (AnyConditionSelected)
                {
                    var selectedConditions = Events.SelectMany(e => e.Conditions).Where(e => e.IsSelected).ToList();
                    if (selectedConditions.Count > 0)
                    {
                        SmartEvent fakeEvent = new(-1) {ReadableHint = ""};
                        foreach (SmartCondition c in selectedConditions)
                            fakeEvent.Conditions.Add(c.Copy());

                        string lines = string.Join("\n",
                            fakeEvent.ToConditionLines(script.EntryOrGuid, script.SourceType, 0).Select(s => s.ToSqlString()));
                        Clipboard.SetText("conditions:\n" + lines);
                    }
                }
            });
            CutCommand = new DelegateCommand(() =>
            {
                CopyCommand.Execute();
                DeleteSelected.Execute();
            });
            PasteCommand = new DelegateCommand(() =>
            {
                if (string.IsNullOrEmpty(Clipboard.GetText()))
                    return;
                
                var content = (Clipboard.GetText() ?? "");
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

            token = eventAggregator.GetEvent<EventRequestGenerateSql>()
                .Subscribe(args =>
                {
                    if (args.Item is SmartScriptSolutionItem)
                    {
                        SmartScriptSolutionItem itemm = args.Item as SmartScriptSolutionItem;
                        if (itemm.Entry == item.Entry && itemm.SmartType == item.SmartType)
                            args.Sql = new SmartScriptExporter(script, smartFactory).GetSql();
                    }
                });
        }

        public string Name => itemNameRegistry.GetName(item);

        public ObservableCollection<SmartEvent> Events => script.Events;

        public CompositeCollection Together { get; } = new();

        public SmartEvent SelectedItem => Events.FirstOrDefault(ev => ev.IsSelected);

        public DelegateCommand EditEvent { get; set; }

        public DelegateCommand DeselectAll { get; set; }
        public DelegateCommand DeselectAllButConditions { get; set; }
        public DelegateCommand DeselectAllButActions { get; set; }
        public DelegateCommand DeselectActions { get; set; }

        public DelegateCommand<DropActionsConditionsArgs> OnDropConditions { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropActions { get; set; }
        public DelegateCommand<int?> OnDropItems { get; set; }

        public DelegateCommand<NewActionViewModel> AddAction { get; set; }
        public DelegateCommand<NewConditionViewModel> AddCondition { get; set; }

        public DelegateCommand<SmartAction> DeleteAction { get; set; }

        public DelegateCommand<SmartAction> EditAction { get; set; }
        public DelegateCommand<SmartCondition> EditCondition { get; 
        }
        public DelegateCommand AddEvent { get; set; }

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
        public ICommand CloseCommand { get; set; }

        public void Dispose()
        {
            token.Dispose();
        }

        internal void SetSolutionItem(SmartScriptSolutionItem item)
        {
            Debug.Assert(this.item == null);
            this.item = item;
            string name = itemNameRegistry.GetName(item);
            Title = name;
            
            script = new SmartScript(this.item, smartFactory);
            
            Together.Add(new CollectionContainer {Collection = script.Events});
            Together.Add(new CollectionContainer {Collection = new List<object> {new NewActionViewModel(), new NewConditionViewModel()}});
            script.Events.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SmartEvent t in args.NewItems)
                    {
                        Together.Add(new CollectionContainer {Collection = t.Actions});
                        Together.Add(new CollectionContainer {Collection = t.Conditions});
                    }
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (SmartEvent t in args.OldItems)
                    {
                        foreach (CollectionContainer a in Together)
                        {
                            if (a.Collection == t.Actions || a.Collection == t.Conditions)
                            {
                                Together.Remove(a);
                                break;
                            }
                        }
                    }
                }
            };
            foreach (SmartEvent t in script.Events)
            {
                Together.Add(new CollectionContainer {Collection = t.Actions});
                Together.Add(new CollectionContainer {Collection = t.Conditions});
            }
            
            taskRunner.ScheduleTask($"Loading script {name}", AsyncLoad);
        }

        private async Task AsyncLoad()
        {
            var lines = database.GetScriptFor(this.item.Entry, this.item.SmartType);
            var conditions = database.GetConditionsFor(SmartConstants.ConditionSourceSmartScript, this.item.Entry, (int)this.item.SmartType);
            script.Load(lines, conditions);
            IsLoading = false;
            History.AddHandler(new SaiHistoryHandler(script));
        }
        
        private async Task SaveAllToDb()
        {
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving to database"));

            var (lines, conditions) = script.ToSmartScriptLinesNoMetaActions(smartFactory);
            
            await database.InstallScriptFor(item.Entry, item.SmartType, lines);
            
            await database.InstallConditions(conditions, 
                IDatabaseProvider.ConditionKeyMask.SourceEntry | IDatabaseProvider.ConditionKeyMask.SourceId,
                new IDatabaseProvider.ConditionKey(SmartConstants.ConditionSourceSmartScript, null, item.Entry, (int)item.SmartType));

            statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));
            
            History.MarkAsSaved();
        }

        private void DeleteActionCommand(SmartAction obj)
        {
            obj.Parent.Actions.Remove(obj);
        }

        private void AddActionCommand(NewActionViewModel obj)
        {
            SmartEvent e = obj.Event;
            if (e == null)
                return;
            int? sourceId = smartTypeListProvider.Get(SmartType.SmartSource,
                data =>
                {
                    if (data.IsOnlyTarget)
                        return false;

                    return data.UsableWithEventTypes == null || data.UsableWithEventTypes.Contains(script.SourceType);
                });

            if (!sourceId.HasValue)
                return;

            int? actionId = smartTypeListProvider.Get(SmartType.SmartAction,
                data =>
                {
                    return (data.UsableWithEventTypes == null || data.UsableWithEventTypes.Contains(script.SourceType)) &&
                           (!data.ImplicitSource || sourceId.Value <= 1 /* @todo: remove this const: this is none or self */
                           );
                });

            if (!actionId.HasValue)
                return;

            SmartGenericJsonData actionData = smartDataManager.GetRawData(SmartType.SmartAction, actionId.Value);

            SmartTarget target = null;

            if (actionData.UsesTarget && !actionData.TargetIsSource)
            {
                int? targetId = smartTypeListProvider.Get(SmartType.SmartTarget,
                    data =>
                    {
                        return (data.UsableWithEventTypes == null || data.UsableWithEventTypes.Contains(script.SourceType)) &&
                               (actionData.Targets == null || actionData.Targets.Intersect(data.Types).Any());
                    });

                if (!targetId.HasValue)
                    return;

                target = smartFactory.TargetFactory(targetId.Value);
            }
            else if (actionData.TargetIsSource)
            {
                target = smartFactory.TargetFactory(sourceId.Value);
                sourceId = 0;
            }
            else
                target = smartFactory.TargetFactory(0);

            if (actionData.ImplicitSource)
                sourceId = 0;

            SmartSource source = smartFactory.SourceFactory(sourceId.Value);

            SmartAction ev = smartFactory.ActionFactory(actionId.Value, source, target);
            if (EditActionCommand(ev))
                e.Actions.Add(ev);
        }

        private void AddConditionCommand(NewConditionViewModel obj)
        {
            SmartEvent e = obj.Event;
            if (e == null)
                return;

            int? conditionId = smartTypeListProvider.Get(SmartType.SmartCondition, _ => true);

            if (!conditionId.HasValue)
                return;

            var conditionData = conditionDataManager.GetConditionData(conditionId.Value);

            SmartCondition ev = smartFactory.ConditionFactory(conditionId.Value);
            
            if ((conditionData.Parameters?.Count ?? 0) == 0 || EditConditionCommand(ev))
                e.Conditions.Add(ev);
        }

        private void AddEventCommand()
        {
            DeselectAll.Execute();
            int? id = smartTypeListProvider.Get(SmartType.SmartEvent,
                data => { return data.ValidTypes == null || data.ValidTypes.Contains(script.SourceType); });

            if (id.HasValue)
            {
                SmartEvent ev = smartFactory.EventFactory(id.Value);
                if (EditEventCommand(ev))
                    script.Events.Add(ev);
            }
        }

        private bool EditActionCommand(SmartAction originalAction)
        {
            SmartAction obj = originalAction.Copy();

            SmartGenericJsonData actionData = smartDataManager.GetRawData(SmartType.SmartAction, originalAction.Id);

            var parametersList = new List<(Parameter, string)>();
            var floatParametersList = new List<(FloatParameter, string)>();
            var stringParametersList = new List<(StringParameter, string)>();

            for (var i = 0; i < obj.Source.ParametersCount; ++i)
            {
                if (!obj.Source.GetParameter(i).Name.Equals("empty"))
                    parametersList.Add((obj.Source.GetParameter(i), "Source"));
            }

            for (var i = 0; i < obj.ParametersCount; ++i)
            {
                if (!obj.GetParameter(i).Name.Equals("empty"))
                    parametersList.Add((obj.GetParameter(i), "Action"));
            }

            for (var i = 0; i < obj.Target.ParametersCount; ++i)
            {
                if (!obj.Target.GetParameter(i).Name.Equals("empty"))
                    parametersList.Add((obj.Target.GetParameter(i), "Target"));
            }

            if (actionData.UsesTargetPosition)
            {
                for (var i = 0; i < 4; ++i)
                    floatParametersList.Add((obj.Target.Position[i], "Target"));
            }

            StringParameter comment = null;
            if (actionData.Id == SmartConstants.ActionComment)
            {
                comment = new StringParameter("Comment");
                comment.Value = originalAction.Comment;
                comment.OnValueChanged += (s, v) => obj.Comment = comment.Value;
                stringParametersList = new List<(StringParameter, string)>() {(comment, "Comment")};
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, obj, parametersList, floatParametersList, stringParametersList);

            var result = windowManager.ShowDialog(viewModel);
            if (result)
            {
                using (originalAction.BulkEdit("Edit action " + obj.Readable))
                {
                    for (var i = 0; i < originalAction.Target.Position.Length; ++i)
                        originalAction.Target.Position[i].Value = obj.Target.Position[i].Value;

                    for (var i = 0; i < originalAction.Target.ParametersCount; ++i)
                        originalAction.Target.SetParameter(i, obj.Target.GetParameter(i).Value);

                    for (var i = 0; i < originalAction.Source.ParametersCount; ++i)
                        originalAction.Source.SetParameter(i, obj.Source.GetParameter(i).Value);

                    for (var i = 0; i < originalAction.ParametersCount; ++i)
                        originalAction.SetParameter(i, obj.GetParameter(i).Value);

                    if (comment != null)
                        originalAction.Comment = comment.Value;
                }
            }

            viewModel.Dispose();
            return result;
        }

        private bool EditConditionCommand(SmartCondition originalCondition)
        {
            SmartCondition obj = originalCondition.Copy();

            var parametersList = new List<(Parameter, string)>();

            parametersList.Add((obj.Inverted, "General"));
            parametersList.Add((obj.ConditionTarget, "General"));
            
            for (var i = 0; i < obj.ParametersCount; ++i)
            {                    
                if (!obj.GetParameter(i).Name.Equals("empty"))
                    parametersList.Add((obj.GetParameter(i), "Condition"));
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, obj, parametersList);
            bool result = windowManager.ShowDialog(viewModel);
            if (result)
            {
                using (originalCondition.BulkEdit("Edit condition " + obj.Readable))
                {
                    originalCondition.Inverted.SetValue(obj.Inverted.Value);
                    originalCondition.ConditionTarget.SetValue(obj.ConditionTarget.Value);
                    for (var i = 0; i < originalCondition.ParametersCount; ++i)
                        originalCondition.SetParameter(i, obj.GetParameter(i).Value);
                }
            }

            viewModel.Dispose();
            return result;
        }

        private void EditEventCommand()
        {
            if (SelectedItem != null)
                EditEventCommand(SelectedItem);
        }

        private bool EditEventCommand(SmartEvent originalEvent)
        {
            SmartEvent ev = originalEvent.ShallowCopy();

            var parametersList = new List<(Parameter, string)>();
            parametersList.Add((ev.Chance, "General"));
            parametersList.Add((ev.Flags, "General"));
            parametersList.Add((ev.Phases, "General"));
            parametersList.Add((ev.CooldownMax, "General"));
            parametersList.Add((ev.CooldownMin, "General"));

            for (var i = 0; i < ev.ParametersCount; ++i)
            {
                if (!ev.GetParameter(i).Name.Equals("empty"))
                    parametersList.Add((ev.GetParameter(i), "Event specific"));
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, ev, parametersList);
            bool result = windowManager.ShowDialog(viewModel);
            if (result)
            {
                using (originalEvent.BulkEdit("Edit event " + ev.Readable))
                {
                    originalEvent.Chance.SetValue(ev.Chance.Value);
                    originalEvent.Flags.SetValue(ev.Flags.Value);
                    originalEvent.Phases.SetValue(ev.Phases.Value);
                    originalEvent.CooldownMax.SetValue(ev.CooldownMax.Value);
                    originalEvent.CooldownMin.SetValue(ev.CooldownMin.Value);
                    for (var i = 0; i < originalEvent.ParametersCount; ++i)
                        originalEvent.SetParameter(i, ev.GetParameter(i).Value);
                }
            }

            viewModel.Dispose();
            return result;
        }
    }
}