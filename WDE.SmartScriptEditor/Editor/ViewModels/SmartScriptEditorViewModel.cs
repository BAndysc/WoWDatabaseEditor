using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
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
using WDE.Common.Utils;
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
        private readonly ISmartDataManager smartDataManager;
        private readonly ISmartFactory smartFactory;
        private readonly ISmartTypeListProvider smartTypeListProvider;
        private readonly IStatusBar statusbar;
        private readonly SubscriptionToken token;

        private SmartScriptSolutionItem item;

        private SmartScript script;

        public SmartScriptEditorViewModel(IHistoryManager history,
            IDatabaseProvider database,
            IEventAggregator eventAggregator,
            ISmartDataManager smartDataManager,
            ISmartFactory smartFactory,
            IItemFromListProvider itemFromListProvider,
            ISmartTypeListProvider smartTypeListProvider,
            IStatusBar statusbar,
            ISolutionItemNameRegistry itemNameRegistry)
        {
            History = history;
            this.database = database;
            this.smartDataManager = smartDataManager;
            this.smartFactory = smartFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.smartTypeListProvider = smartTypeListProvider;
            this.statusbar = statusbar;
            this.itemNameRegistry = itemNameRegistry;

            EditEvent = new DelegateCommand(EditEventCommand);
            DeselectActions = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    if (!e.IsSelected)
                    {
                        foreach (SmartAction a in e.Actions)
                            a.IsSelected = false;
                    }
                }
            });
            DeselectAll = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                {
                    foreach (SmartAction a in e.Actions)
                        a.IsSelected = false;

                    e.IsSelected = false;
                }
            });
            DeselectAllEvents = new DelegateCommand(() =>
            {
                foreach (SmartEvent e in Events)
                    e.IsSelected = false;
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
            OnDropActions = new DelegateCommand<DropActionsArgs>(data =>
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
            EditAction = new DelegateCommand<SmartAction>(action => EditActionCommand(action));
            AddEvent = new DelegateCommand(AddEventCommand);
            AddAction = new DelegateCommand<NewActionViewModel>(AddActionCommand);

            SaveCommand = new AsyncAutoCommand(SaveAllToDb,
                null,
                e =>
                {
                    statusbar.PublishNotification(new PlainNotification(NotificationType.Error,
                        "Error while saving script to the database: " + e.Message));
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

                        if (nextSelect.HasValue && nextSelect.Value.actionIndex < Events[nextSelect.Value.eventIndex].Actions.Count)
                            Events[nextSelect.Value.eventIndex].Actions[nextSelect.Value.actionIndex].IsSelected = true;
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
            });

            CopyCommand = new DelegateCommand(() =>
            {
                var selectedEvents = Events.Where(e => e.IsSelected).ToList();
                if (selectedEvents.Count > 0)
                {
                    string lines = string.Join("\n",
                        selectedEvents.SelectMany((e, index) => e.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, index))
                            .Select(s => s.ToSqlString()));
                    Clipboard.SetText(lines);
                }
                else
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
            });
            CutCommand = new DelegateCommand(() =>
            {
                CopyCommand.Execute();
                DeleteSelected.Execute();
            });
            PasteCommand = new DelegateCommand(() =>
            {
                var lines = (Clipboard.GetText() ?? "").Split('\n')
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
                            foreach (SmartAction smartAction in lines.Select(line => script.SafeActionFactory(line)))
                            {
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
                                    else
                                        index--;
                                    //Events.RemoveAt(i);
                                }
                            }

                            if (!index.HasValue)
                                index = Events.Count;
                            script.InsertFromClipboard(index.Value, lines);
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
        public DelegateCommand DeselectAllEvents { get; set; }
        public DelegateCommand DeselectActions { get; set; }

        public DelegateCommand<DropActionsArgs> OnDropActions { get; set; }
        public DelegateCommand<int?> OnDropItems { get; set; }

        public DelegateCommand<NewActionViewModel> AddAction { get; set; }

        public DelegateCommand<SmartAction> DeleteAction { get; set; }

        public DelegateCommand<SmartAction> EditAction { get; set; }

        public DelegateCommand AddEvent { get; set; }

        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand RedoCommand { get; set; }

        public DelegateCommand CopyCommand { get; set; }
        public DelegateCommand CutCommand { get; set; }
        public DelegateCommand PasteCommand { get; set; }
        public AsyncAutoCommand SaveCommand { get; set; }
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

        public string Title { get; set; }
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
            Title = itemNameRegistry.GetName(item);

            var lines = database.GetScriptFor(this.item.Entry, this.item.SmartType);
            script = new SmartScript(this.item, smartFactory);
            script.Load(lines);

            Together.Add(new CollectionContainer {Collection = new List<NewActionViewModel> {new()}});
            Together.Add(new CollectionContainer {Collection = script.Events});
            script.Events.CollectionChanged += (sender, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (SmartEvent t in args.NewItems)
                        Together.Add(new CollectionContainer {Collection = t.Actions});
                }
                else if (args.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (SmartEvent t in args.OldItems)
                    {
                        foreach (CollectionContainer a in Together)
                        {
                            if (a.Collection == t.Actions)
                            {
                                Together.Remove(a);
                                break;
                            }
                        }
                    }
                }
            };
            foreach (SmartEvent t in script.Events)
                Together.Add(new CollectionContainer {Collection = t.Actions});

            History.AddHandler(new SaiHistoryHandler(script));
        }

        private async Task SaveAllToDb()
        {
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving to database"));

            var lines = script.ToWaitFreeSmartScriptLines(smartFactory);
            await database.InstallScriptFor(item.Entry, item.SmartType, lines);

            statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));
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

        private void AddEventCommand()
        {
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
            //@todo: constructing view in place is veeery ugly
            ParametersEditView v = new();
            SmartAction obj = originalAction.Copy();

            var paramss = new List<KeyValuePair<Parameter, string>>();

            for (var i = 0; i < obj.Source.ParametersCount; ++i)
            {
                if (!obj.Source.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.Source.GetParameter(i), "Source"));
            }

            for (var i = 0; i < obj.ParametersCount; ++i)
            {
                if (!obj.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.GetParameter(i), "Action"));
            }

            for (var i = 0; i < obj.Target.ParametersCount; ++i)
            {
                if (!obj.Target.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.Target.GetParameter(i), "Target"));
            }

            for (var i = 0; i < 4; ++i)
            {
                int j = i;
                Parameter wrapper = new FloatIntParameter(obj.Target.Position[i].Name);
                wrapper.SetValue((int) (obj.Target.Position[i].GetValue() * 1000));
                wrapper.OnValueChanged += (sender, value) => obj.Target.Position[j].SetValue(wrapper.GetValue() / 1000.0f);
                paramss.Add(new KeyValuePair<Parameter, string>(wrapper, "Target"));
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, obj, paramss);
            v.DataContext = viewModel;
            bool result = v.ShowDialog() ?? false;
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
            //@todo: constructing view in place is veeery ugly
            SmartEvent ev = originalEvent.ShallowCopy();

            ParametersEditView v = new();
            var paramss = new List<KeyValuePair<Parameter, string>>();
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Chance, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Flags, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Phases, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.CooldownMax, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.CooldownMin, "General"));

            for (var i = 0; i < ev.ParametersCount; ++i)
            {
                if (!ev.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(ev.GetParameter(i), "Event specific"));
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider, ev, paramss);
            v.DataContext = viewModel;
            bool result = v.ShowDialog() ?? false;
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