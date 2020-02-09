using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.Views;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using WDE.Common.Providers;
using WDE.Common.Solution;
using System.Diagnostics;
using WDE.Conditions.Data;
using WDE.Conditions.Model;
using WDE.Conditions.Providers;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public class SmartScriptEditorViewModel : BindableBase
    {
        private readonly IDatabaseProvider database;
        private readonly IHistoryManager history;
        private readonly ISmartDataManager smartDataManager;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISmartFactory smartFactory;
        private readonly ISmartTypeListProvider smartTypeListProvider;
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly IConditionDataManager conditionDataManager;
        private readonly IParameterFactory parameterFactory;
        private readonly IConditionsEditViewProvider conditionsEditViewProvider;

        private SmartScriptSolutionItem _item;

        public string Name => itemNameRegistry.GetName(_item);

        private SmartScript script;

        public ObservableCollection<SmartEvent> Events => script.Events;

        public IHistoryManager History => history;

        public SmartEvent SelectedItem { get; set; }
        public SmartAction SelectedAction { get; set; }

        public DelegateCommand EditEvent { get; set; }

        public DelegateCommand<SmartEvent> AddAction { get; set; }

        public DelegateCommand<SmartAction> DeleteAction { get; set; }

        public DelegateCommand<SmartAction> EditAction { get; set; }
        
        public DelegateCommand AddEvent { get; set; }

        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand RedoCommand { get; set; }

        public DelegateCommand SaveCommand { get; set; }

        public DelegateCommand DeleteEvent { get; set; }

        public DelegateCommand DeleteEventOrAction { get; set; }

        public SmartScriptEditorViewModel(IHistoryManager history, IDatabaseProvider database, IEventAggregator eventAggregator, ISmartDataManager smartDataManager, ISmartFactory smartFactory,
            IItemFromListProvider itemFromListProvider, ISmartTypeListProvider smartTypeListProvider, ISolutionItemNameRegistry itemNameRegistry, IConditionDataManager conditionDataManager,
            IParameterFactory parameterFactory, IConditionsEditViewProvider conditionsEditViewProvider)
        {
            this.history = history;
            this.database = database;
            this.smartDataManager = smartDataManager;
            this.smartFactory = smartFactory;
            this.itemFromListProvider = itemFromListProvider;
            this.smartTypeListProvider = smartTypeListProvider;
            this.itemNameRegistry = itemNameRegistry;
            this.conditionDataManager = conditionDataManager;
            this.parameterFactory = parameterFactory;
            this.conditionsEditViewProvider = conditionsEditViewProvider;

            EditEvent = new DelegateCommand(EditEventCommand);
            EditAction = new DelegateCommand<SmartAction>(EditActionCommand);
            AddEvent = new DelegateCommand(AddEventCommand);
            AddAction = new DelegateCommand<SmartEvent>(AddActionCommand);

            SaveCommand = new DelegateCommand(SaveAllToDb);

            DeleteAction = new DelegateCommand<SmartAction>(DeleteActionCommand);
            DeleteEvent = new DelegateCommand(DeleteEventCommand);
            
            UndoCommand = new DelegateCommand(history.Undo, () => history.CanUndo);
            RedoCommand = new DelegateCommand(history.Redo, () => history.CanRedo);

            DeleteEventOrAction = new DelegateCommand(DeleteEventOrActionCommand);

            this.history.PropertyChanged += (sender, args) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
            };
            
            eventAggregator.GetEvent<EventRequestGenerateSql>().Subscribe((args) =>
            {
                if (args.Item is SmartScriptSolutionItem)
                {
                    var itemm = args.Item as SmartScriptSolutionItem;
                    if (itemm.Entry == _item.Entry && itemm.SmartType == _item.SmartType)
                    {
                        args.Sql = new SmartScriptExporter(script, smartFactory).GetSql();
                    }
                }
            });
        }

        internal void SetSolutionItem(SmartScriptSolutionItem item)
        {
            Debug.Assert(_item == null);
            _item = item;

            var lines = database.GetScriptFor(_item.Entry, _item.SmartType);
            var conditions = database.GetConditionsFor(Condition.CONDITION_SOURCE_SMART_EVENT, _item.Entry, (int)_item.SmartType);
            script = new SmartScript(_item, smartFactory, conditionDataManager);
            script.Load(lines, conditions);

            history.AddHandler(new SaiHistoryHandler(script));
        }

        private void DeleteEventCommand()
        {
            if (SelectedItem != null)
                script.Events.Remove(SelectedItem);
        }

        private void SaveAllToDb()
        {

            List<AbstractSmartScriptLine> lines = new List<AbstractSmartScriptLine>();

            int eventId = 1;

            foreach (SmartEvent e in script.Events)
            {
                if (e.Actions.Count == 0)
                    continue;

                e.ActualId = eventId;
                lines.Add(GenerateSingleSai(eventId, e, e.Actions[0], (e.Actions.Count == 1 ? 0 : eventId + 1)));

                eventId++;

                for (int index = 1; index < e.Actions.Count; ++index)
                {
                    lines.Add(GenerateSingleSai(eventId, smartFactory.EventFactory(61),
                        e.Actions[index], (e.Actions.Count - 1 == index ? 0 : eventId + 1)));
                    eventId++;
                }
            }

            database.InstallScriptFor(_item.Entry, _item.SmartType, lines);
        }

        private AbstractSmartScriptLine GenerateSingleSai(int eventId, SmartEvent ev, SmartAction action, int link = 0, string comment = null)
        {
            AbstractSmartScriptLine line = new AbstractSmartScriptLine
            {
                EntryOrGuid = _item.Entry,
                ScriptSourceType = (int)_item.SmartType,
                Id = eventId,
                Link = link,
                EventType = ev.Id,
                EventPhaseMask = ev.Phases.Value,
                EventChance = ev.Chance.Value,
                EventFlags = ev.Flags.Value,
                EventParam1 = ev.GetParameter(0).Value,
                EventParam2 = ev.GetParameter(1).Value,
                EventParam3 = ev.GetParameter(2).Value,
                EventParam4 = ev.GetParameter(3).Value,
                EventCooldownMin = ev.CooldownMin.Value,
                EventCooldownMax = ev.CooldownMax.Value,
                ActionType = action.Id,
                ActionParam1 = action.GetParameter(0).Value,
                ActionParam2 = action.GetParameter(1).Value,
                ActionParam3 = action.GetParameter(2).Value,
                ActionParam4 = action.GetParameter(3).Value,
                ActionParam5 = action.GetParameter(4).Value,
                ActionParam6 = action.GetParameter(5).Value,
                SourceType = action.Source.Id,
                SourceParam1 = action.Source.GetParameter(0).Value,
                SourceParam2 = action.Source.GetParameter(1).Value,
                SourceParam3 = action.Source.GetParameter(2).Value,
                SourceConditionId = action.Source.Condition.Value,
                TargetType = action.Target.Id,
                TargetParam1 = action.Target.GetParameter(0).Value,
                TargetParam2 = action.Target.GetParameter(1).Value,
                TargetParam3 = action.Target.GetParameter(2).Value,
                TargetConditionId = action.Target.Condition.Value,
                TargetX = action.Target.X,
                TargetY = action.Target.Y,
                TargetZ = action.Target.Z,
                TargetO = action.Target.O,
                Comment = ev.Readable + " - " + action.Readable
            };
            
            return line;
        }

        private void DeleteActionCommand(SmartAction obj)
        {
            obj.Parent.Actions.Remove(obj);
        }

        private void AddActionCommand(SmartEvent obj)
        {
            int? sourceId = smartTypeListProvider.Get(SmartType.SmartSource, data =>
                {
                    if (data.IsOnlyTarget)
                        return false;

                    return data.UsableWithEventTypes == null || data.UsableWithEventTypes.Contains(script.SourceType);
                }
            );

            if (!sourceId.HasValue)
                return;

            int? actionId = smartTypeListProvider.Get(SmartType.SmartAction, data =>
            {
                return (data.UsableWithEventTypes == null || data.UsableWithEventTypes.Contains(script.SourceType)) &&
                        (!data.ImplicitSource || sourceId.Value <= 1 /* @todo: remove this const: this is none or self */);
            });

            if (!actionId.HasValue)
                return;

            var actionData = smartDataManager.GetRawData(SmartType.SmartAction, actionId.Value);

            SmartTarget target = null;

            if (actionData.UsesTarget && !actionData.TargetIsSource)
            {
                int? targetId = smartTypeListProvider.Get(SmartType.SmartTarget, data =>
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
            EditActionCommand(ev);
            obj.Actions.Add(ev);
            
        }

        private void AddEventCommand()
        {
            int? id = smartTypeListProvider.Get(SmartType.SmartEvent, data =>
            {
                return data.ValidTypes == null || data.ValidTypes.Contains(script.SourceType);
            });

            if (id.HasValue)
            {
                SmartEvent ev = smartFactory.EventFactory(id.Value);
                EditEventCommand(ev);
                script.Events.Add(ev);
            }
        }

        private void EditActionCommand(SmartAction obj)
        {
            ParametersEditView v = new ParametersEditView(conditionsEditViewProvider);
            List<KeyValuePair<Parameter, string>> paramss = new List<KeyValuePair<Parameter, string>>();
           
            for (int i = 0; i < obj.Source.ParametersCount; ++i)
                if (!obj.Source.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.Source.GetParameter(i), "Source"));
            
            for (int i = 0; i < obj.ParametersCount; ++i)
                if (!obj.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.GetParameter(i), "Action"));
            
            for (int i = 0; i < obj.Target.ParametersCount; ++i)
                if (!obj.Target.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(obj.Target.GetParameter(i), "Target"));

            for (int i = 0; i < 4; ++i)
            {
                int j = i;
                Parameter wrapper = new FloatIntParameter(obj.Target.Position[i].Name);
                wrapper.SetValue((int)(obj.Target.Position[i].GetValue()*1000));
                wrapper.OnValueChanged += (sender, value) => obj.Target.Position[j].SetValue(wrapper.GetValue() / 1000.0f);
                paramss.Add(new KeyValuePair<Parameter, string>(wrapper, "Target"));
            }

            // Hide Conditions stuff for SmartAction, because there is no ConditionType for these
            v.conditionsTextBlock.Visibility = System.Windows.Visibility.Hidden;
            v.editConditionsButton.Visibility = System.Windows.Visibility.Hidden;
            v.DataContext = new ParametersEditViewModel(itemFromListProvider, obj, paramss, null, conditionDataManager);
            v.ShowDialog();
        }

        private void EditEventCommand()
        {
            EditEventCommand(SelectedItem);
        }

        private void EditEventCommand(SmartEvent ev)
        {
            ParametersEditView v = new ParametersEditView(conditionsEditViewProvider);
            List<KeyValuePair<Parameter, string>> paramss = new List<KeyValuePair<Parameter, string>>();
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Chance, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Flags, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.Phases, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.CooldownMax, "General"));
            paramss.Add(new KeyValuePair<Parameter, string>(ev.CooldownMin, "General"));

            for (int i = 0; i < ev.ParametersCount; ++i)
                if (!ev.GetParameter(i).Name.Equals("empty"))
                    paramss.Add(new KeyValuePair<Parameter, string>(ev.GetParameter(i), "Event specific"));

            v.DataContext = new ParametersEditViewModel(itemFromListProvider, ev, paramss, ev.Conditions, conditionDataManager);
            v.ShowDialog();
        }

        private void DeleteEventOrActionCommand()
        {
            if (SelectedAction != null)
                DeleteActionCommand(SelectedAction);
            else if (SelectedItem != null)
                DeleteEventCommand();
        }
    }
}
