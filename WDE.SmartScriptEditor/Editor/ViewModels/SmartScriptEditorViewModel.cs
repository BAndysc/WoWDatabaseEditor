using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Outliner;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor.UserControls;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;
using WDE.SmartScriptEditor.Exporter;
using WDE.SmartScriptEditor.Models;
using WDE.SmartScriptEditor.History;
using WDE.SmartScriptEditor.Inspections;
using WDE.SmartScriptEditor.Services;
using WDE.SmartScriptEditor.Settings;
using WDE.SqlQueryGenerator;

namespace WDE.SmartScriptEditor.Editor.ViewModels
{
    public partial class SmartScriptEditorViewModel : ObservableBase, ISolutionItemDocument, IProblemSourceDocument, IOutlinerSourceDocument
    {
        private readonly ISmartScriptDatabaseProvider smartScriptDatabase;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly ISolutionItemNameRegistry itemNameRegistry;
        private readonly ITaskRunner taskRunner;
        private readonly IToolSmartEditorViewModel smartEditorViewModel;
        private readonly ISmartScriptExporter smartScriptExporter;
        private readonly ISmartScriptImporter smartScriptImporter;
        private readonly IEditorFeatures editorFeatures;
        private readonly ITeachingTipService teachingTipService;
        private readonly IMainThread mainThread;
        private readonly IConditionEditService conditionEditService;
        private readonly ICurrentCoreVersion currentCoreVersion;
        private readonly ISmartScriptInspectorService inspectorService;
        private readonly IParameterPickerService parameterPickerService;
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly ISmartEditorExtension editorExtension;
        private readonly IGeneralSmartScriptSettingsProvider preferences;
        private readonly ISmartDataManager smartDataManager;
        private readonly IConditionDataManager conditionDataManager;
        private readonly ISmartFactory smartFactory;
        private readonly ISmartTypeListProvider smartTypeListProvider;
        private readonly IStatusBar statusbar;
        private readonly IWindowManager windowManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IOutlinerService outlinerService;

        private ISmartScriptSolutionItem item;
        private SmartScript script;

        public SmartTeachingTips TeachingTips { get; private set; }

        public ImageUri? Icon { get; }

        private bool isLoading = true;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }

        [Notify] private string? searchText;
        
        [Notify] private float scale = 1.0f;

        [Notify] private bool hideComments = false;

        [Notify] private bool hideConditions = false;
        
        public ICommand SaveDefaultScaleCommand { get; }

        public SmartScript Script
        {
            get => script;
            set => SetProperty(ref script, value);
        }

        private class ClipboardEvents
        {
            public List<AbstractSmartScriptLine> Lines { get; set; } = new();
            public List<AbstractConditionLine> Conditions { get; set; } = new();
            public List<AbstractConditionLine> TargetConditions { get; set; } = new();
        }
        
        public IList<SmartExtensionCommand> ExtensionCommands { get; }

        [Notify] private HighlightViewModel selectedHighlighter; 
        public IReadOnlyList<HighlightViewModel> Highlighters => highlighter.Highlighters;

        public SmartScriptEditorViewModel(ISmartScriptSolutionItem item,
            IHistoryManager history,
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
            ISmartScriptImporter smartScriptImporter,
            IEditorFeatures editorFeatures,
            ITeachingTipService teachingTipService,
            IMainThread mainThread,
            ISolutionItemIconRegistry iconRegistry,
            IConditionEditService conditionEditService,
            ICurrentCoreVersion currentCoreVersion,
            ISmartScriptInspectorService inspectorService,
            IParameterPickerService parameterPickerService,
            IMySqlExecutor mySqlExecutor,
            ISmartEditorExtension editorExtension,
            IGeneralSmartScriptSettingsProvider preferences,
            IOutlinerService outlinerService,
            ISmartScriptOutlinerModel outlinerData,
            ISmartHighlighter highlighter)
        {
            History = history;
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
            this.smartScriptImporter = smartScriptImporter;
            this.editorFeatures = editorFeatures;
            this.teachingTipService = teachingTipService;
            this.mainThread = mainThread;
            this.conditionEditService = conditionEditService;
            this.currentCoreVersion = currentCoreVersion;
            this.inspectorService = inspectorService;
            this.parameterPickerService = parameterPickerService;
            this.mySqlExecutor = mySqlExecutor;
            this.editorExtension = editorExtension;
            this.preferences = preferences;
            this.conditionDataManager = conditionDataManager;
            this.outlinerService = outlinerService;
            this.outlinerData = outlinerData;
            this.highlighter = highlighter;
            selectedHighlighter = highlighter.Highlighters[0];
            script = null!;
            this.item = null!;
            TeachingTips = null!;
            title = "";
            Icon = iconRegistry.GetIcon(item);
            scale = preferences.DefaultScale;
            
            On(() => SelectedHighlighter, v =>
            {
                if (script == null)
                    return;
                foreach (var e in Events)
                {
                    RecolorEvent(e);
                }
            });

            ExtensionCommands = editorExtension?.Commands?.ToList() ?? new List<SmartExtensionCommand>();
            
            CloseCommand = new AsyncCommand(async () =>
            {
                if (smartEditorViewModel.CurrentScript == Script)
                    smartEditorViewModel.ShowEditor(null, null);
            });
            EditEvent = new DelegateCommand(EditSelectedEventsCommand);
            DeselectAllButGroups = new DelegateCommand(() =>
            {
                foreach (var gv in script.GlobalVariables)
                    gv.IsSelected = false;
                foreach (SmartEvent e in Events)
                {
                    if (e.IsEvent)
                    {
                        foreach (var a in e.Actions)
                            a.IsSelected = false;
                        foreach (var c in e.Conditions)
                            c.IsSelected = false;
                        e.IsSelected = false;
                    }
                }
            });
            DeselectAllButEvents = new DelegateCommand(() =>
            {
                foreach (var gv in script.GlobalVariables)
                    gv.IsSelected = false;
                foreach (SmartEvent e in Events)
                {
                    if (!e.IsSelected)
                    {
                        foreach (var a in e.Actions)
                            a.IsSelected = false;
                        foreach (var c in e.Conditions)
                            c.IsSelected = false;
                    }

                    if (e.IsGroup)
                        e.IsSelected = false;
                }
            });
            DeselectAll = new DelegateCommand(() =>
            {
                foreach (var gv in script.GlobalVariables)
                    gv.IsSelected = false;
                
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
                foreach (var gv in script.GlobalVariables)
                    gv.IsSelected = false;
                foreach (SmartEvent e in Events)
                {
                    foreach (var c in e.Conditions)
                        c.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            DeselectAllButGlobalVariables = new DelegateCommand(() =>
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
            DeselectAllButConditions = new DelegateCommand(() =>
            {
                foreach (var gv in script.GlobalVariables)
                    gv.IsSelected = false;
                foreach (SmartEvent e in Events)
                {
                    foreach (var a in e.Actions)
                        a.IsSelected = false;
                        
                    e.IsSelected = false;
                }
            });
            OnDropItems = new DelegateCommand<DropActionsConditionsArgs>(args =>
            {
                if (args == null)
                    return;

                var destIndex = args.EventIndex;
                
                using (script.BulkEdit(args.Move ? "Reorder events" : "Copy events"))
                {
                    var selected = new List<SmartEvent>();
                    int d = destIndex;
                    for (int i = Events.Count - 1; i >= 0; --i)
                    {
                        if (Events[i].IsSelected)
                        {
                            selected.Add(Events[i]);
                            Events[i].IsSelected = false;
                            if (args.Move)
                            {
                                if (i < destIndex)
                                    d--;
                                script.Events.RemoveAt(i);
                            }
                        }
                    }

                    if (d == -1)
                        d = 0;
                    selected.Reverse();
                    foreach (SmartEvent s in selected)
                    {
                        var newEvent = args.Copy ? s.DeepCopy() : s;
                        newEvent.IsSelected = true;
                        script.Events.Insert(d++, newEvent);
                    }
                }
            });
            OnDropGroups = new DelegateCommand<DropActionsConditionsArgs>(args =>
            {
                if (args == null)
                    return;

                bool inGroup = false;
                foreach (var e in Events)
                {
                    if (e.IsBeginGroup && e.IsSelected)
                    {
                        inGroup = true;
                    }

                    if (inGroup && e.IsEndGroup)
                    {
                        e.IsSelected = true;
                        inGroup = false;
                    }

                    if (inGroup && e.IsEvent)
                        e.IsSelected = true;
                }
                OnDropItems.Execute(args);
            });
            OnDropActions = new DelegateCommand<DropActionsConditionsArgs>(data =>
            {
                using (script.BulkEdit(data.Move ? "Reorder actions" : "Copy actions"))
                {
                    if (data.EventIndex < 0 || data.EventIndex >= Events.Count)
                    {
                        Console.WriteLine("Fatal error! event index out of range");
                        return;
                    }
                    var selected = new List<SmartAction>();
                    int d = data.ActionIndex;
                    for (var eventIndex = Events.Count - 1; eventIndex >= 0; eventIndex--)
                    {
                        SmartEvent e = Events[eventIndex];
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
                    foreach (SmartAction s in selected)
                    {
                        SmartAction newAction = data.Copy ? s.Copy() : s;
                        newAction.IsSelected = true;
                        Events[data.EventIndex].Actions.Insert(d++, newAction);
                    }
                }
            });
            OnDropConditions = new DelegateCommand<DropActionsConditionsArgs>(data =>
            {
                using (script.BulkEdit(data.Move ? "Reorder conditions" : "Copy conditions"))
                {
                    if (data.EventIndex < 0 || data.EventIndex >= Events.Count)
                    {
                        Console.WriteLine("Fatal error! event index out of range");
                        return;
                    }
                    var selected = new List<SmartCondition>();
                    int d = data.ActionIndex;
                    for (var eventIndex = Events.Count - 1; eventIndex >= 0; eventIndex--)
                    {
                        SmartEvent e = Events[eventIndex];
                        for (int i = e.Conditions.Count - 1; i >= 0; --i)
                        {
                            if (e.Conditions[i].IsSelected)
                            {
                                selected.Add(e.Conditions[i]);
                                e.Conditions[i].IsSelected = false;
                                if (data.Move)
                                {
                                    if (eventIndex == data.EventIndex && i < data.ActionIndex)
                                        d--;
                                    e.Conditions.RemoveAt(i);   
                                }
                            }
                        }
                    }

                    selected.Reverse();
                    foreach (SmartCondition s in selected)
                    {
                        var newCondition = data.Copy ? s.Copy() : s;
                        newCondition.IsSelected = true;
                        Events[data.EventIndex].Conditions.Insert(d++, newCondition);
                    }
                }
            });

            EditAction = new AsyncAutoCommand(EditSelectedActionsCommand);
            EditCondition = new AsyncAutoCommand(EditSelectedConditionsCommand);
            AddEvent = new AsyncAutoCommand(() => AddEventCommand(null));
            AddAction = new AsyncAutoCommand<NewActionViewModel>(AddActionCommand);
            AddCondition = new AsyncAutoCommand<NewConditionViewModel>(AddConditionCommand);

            DefineGlobalVariable = new AsyncAutoCommand(async () =>
            {
                var variable = new GlobalVariable();
                variable.Name = "New name";
                var vm = new GlobalVariableEditDialogViewModel(variable);
                if (await windowManager.ShowDialog(vm))
                {
                    variable.Key = vm.Key;
                    variable.VariableType = vm.VariableType;
                    variable.Name = vm.Name;
                    variable.Comment = vm.Comment;
                    variable.Entry = vm.Entry;
                    script.GlobalVariables.Add(variable);
                }
            });
            
            DirectEditParameter = new AsyncAutoCommand<object>(async obj =>
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
                else if (obj is MetaSmartSourceTargetEdit sourceTargetEdit)
                {
                    var actionData = smartDataManager.GetRawData(SmartType.SmartAction, sourceTargetEdit.RelatedAction.Id);
                    if (sourceTargetEdit.IsSource)
                    {
                        var newSource = await ShowSourcePicker(sourceTargetEdit.RelatedAction.Parent, actionData);
                        if (newSource.HasValue)
                        {
                            var newId = newSource.Value.Item2
                                ? SmartConstants.SourceStoredObject
                                : newSource.Value.Item1;
                            smartFactory.UpdateSource(sourceTargetEdit.RelatedAction.Source, newId);
                            
                            if (newSource.Value.Item2)
                                sourceTargetEdit.RelatedAction.Source.GetParameter(0).Value = newSource.Value.Item1;
                        }                        
                    }
                    else
                    {
                        var newTarget = await ShowTargetPicker(sourceTargetEdit.RelatedAction.Parent, actionData);
                        if (newTarget.HasValue)
                        {
                            var newId = newTarget.Value.Item2
                                ? SmartConstants.SourceStoredObject
                                : newTarget.Value.Item1;
                            smartFactory.UpdateTarget(sourceTargetEdit.RelatedAction.Target, newId);
                            
                            if (newTarget.Value.Item2)
                                sourceTargetEdit.RelatedAction.Target.GetParameter(0).Value = newTarget.Value.Item1;
                        }   
                    }
                }
            });
            DirectOpenParameter = new AsyncAutoCommand<object>(async obj =>
            {
                if (obj is ParameterWithContext param)
                {
//                    param.
                }
            });
            /*SaveCommand = new AsyncAutoCommand(SaveAllToDb,
                null,
                e =>
                {
                    statusbar.PublishNotification(new PlainNotification(NotificationType.Error,
                        "Error while saving script to the database: " + e.Message));
                });*/
            SaveCommand = new AsyncAutoCommand(() =>
            {
                return taskRunner.ScheduleTask("Save script to database", SaveAllToDb);
            });

            DeleteAction = new DelegateCommand<SmartAction>(DeleteActionCommand);
            DeleteSelected = new DelegateCommand(() =>
            {
                if (AnyGlobalVariableSelected)
                {
                    using (script.BulkEdit("Delete global variables"))
                    {
                        int? nextSelect = FirstSelectedGlobalVariableIndex;
                        if (MultipleGlobalVariablesSelected)
                            nextSelect = null;

                        for (int i = script.GlobalVariables.Count - 1; i >= 0; --i)
                        {
                            if (script.GlobalVariables[i].IsSelected)
                                script.GlobalVariables.RemoveAt(i);
                        }

                        if (nextSelect.HasValue)
                        {
                            if (nextSelect.Value < script.GlobalVariables.Count)
                                script.GlobalVariables[nextSelect.Value].IsSelected = true;
                            else if (nextSelect.Value - 1 >= 0 && nextSelect.Value - 1 < script.GlobalVariables.Count)
                                script.GlobalVariables[nextSelect.Value - 1].IsSelected = true;
                        }
                    }
                }
                else if (AnyGroupSelected)
                {
                    using (script.BulkEdit("Delete groups"))
                    {
                        bool inGroup = false;
                        List<SmartEvent> innerEvents = new();
                        for (int i = 0; i < Events.Count;)
                        {
                            var e = Events[i];
                            if (e.IsBeginGroup && e.IsSelected)
                            {
                                inGroup = true;
                                Events.RemoveAt(i);
                            }
                            else if (e.IsEndGroup && inGroup)
                            {
                                inGroup = false;
                                Events.RemoveAt(i);
                            }
                            else if (e.IsBeginGroup && !e.IsSelected && inGroup)
                            {
                                inGroup = false;
                                i++;
                            }
                            else if (inGroup)
                            {
                                innerEvents.Add(e);
                                i++;
                            }
                            else
                                i++;
                        }

                        DeselectAll.Execute();
                        innerEvents.ForEach(e => e.IsSelected = true);
                    }
                }
                else if (AnyEventSelected)
                {
                    using (script.BulkEdit("Delete events"))
                    {
                        int? nextSelect = GetNextVisibleEvent(FirstSelectedEventIndex, 1);
                        if (MultipleEventsSelected)
                            nextSelect = null;
                        SmartEvent? eventToSelect = null;
                        if (nextSelect.HasValue && nextSelect != -1)
                            eventToSelect = Events[nextSelect.Value];
                        
                        for (int i = Events.Count - 1; i >= 0; --i)
                        {
                            if (Events[i].IsSelected)
                                Events.RemoveAt(i);
                        }

                        DeselectAll.Execute();
                        if (eventToSelect != null)
                            eventToSelect.IsSelected = true;
                    }
                }
                else if (AnyActionSelected)
                {
                    using (script.BulkEdit("Delete actions"))
                    {
                        (int nextEventIndex, int nextActionIndex)? nextSelect = GetNextVisibleAction(FirstSelectedActionIndex.eventIndex, FirstSelectedActionIndex.actionIndex, 1);
                        if (nextSelect.Value.nextEventIndex == FirstSelectedActionIndex.eventIndex && nextSelect.Value.nextActionIndex == FirstSelectedActionIndex.actionIndex)
                            nextSelect = GetNextVisibleAction(FirstSelectedActionIndex.eventIndex, FirstSelectedActionIndex.actionIndex, -1);
                        if (MultipleActionsSelected)
                            nextSelect = null;

                        SmartAction? nextActionToSelect = null;
                        if (nextSelect.HasValue && (nextSelect.Value.nextEventIndex != FirstSelectedActionIndex.eventIndex || nextSelect.Value.nextActionIndex != FirstSelectedActionIndex.actionIndex))
                            nextActionToSelect = Events[nextSelect.Value.nextEventIndex].Actions[nextSelect.Value.nextActionIndex];

                        for (var i = 0; i < Events.Count; ++i)
                        {
                            SmartEvent e = Events[i];
                            for (int j = e.Actions.Count - 1; j >= 0; --j)
                            {
                                if (e.Actions[j].IsSelected)
                                    e.Actions.RemoveAt(j);
                            }
                        }

                        if (nextActionToSelect != null)
                            nextActionToSelect.IsSelected = true;
                    }
                }
                else if (AnyConditionSelected && !editorFeatures.UseExternalConditionsEditor) // with external editor, you may not delete conditions in sai directly
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
                    EditSelectedEventsCommand();
                }
                else if (AnyActionSelected)
                    EditSelectedActionsCommand();
                else if (AnyConditionSelected)
                    EditSelectedConditionsCommand();
            });

            CopyCommand = new AsyncCommand(async () =>
            {
                var selectedEvents = Events.Where(e => e.IsSelected).ToList();
                if (selectedEvents.Count > 0)
                {
                    int targetIndex = 1;
                    var targetConditions = selectedEvents
                        .SelectMany(e => e.Actions)
                        .SelectMany(a => new[] { a.Source, a.Target })
                        .SelectMany(x =>
                        {
                            if (x.Conditions != null && x.Conditions.Count > 0)
                            {
                                x.Condition.Value = targetIndex++;
                                return x.Conditions!.Select(c => new AbstractConditionLine(0, targetIndex - 1, 0, 0, c));
                            }
                            else
                            {
                                x.Condition.Value = 0;
                                return Array.Empty<AbstractConditionLine>();
                            }
                        }).ToList();
                    
                    var eventLines = selectedEvents
                        .SelectMany((e, index) => e.ToSmartScriptLines(script.EntryOrGuid, script.SourceType, index))
                        .ToList();

                    var conditionLines = selectedEvents
                        .SelectMany((e, index) => this.smartScriptExporter.ToDatabaseCompatibleConditions(script, e, index))
                        .ToList();

                    var clibpoardEvents = new ClipboardEvents() { Lines = eventLines, 
                        Conditions = conditionLines.Select(l => new AbstractConditionLine(l)).ToList(),
                        TargetConditions = targetConditions
                    };

                    string serialized = JsonConvert.SerializeObject(clibpoardEvents);
                    clipboard.SetText(serialized);
                }
                else if (AnyActionSelected)
                {
                    int targetIndex = 1;
                    var selectedActions = Events.SelectMany(e => e.Actions).Where(e => e.IsSelected).ToList();
                    if (selectedActions.Count > 0)
                    {
                        var targetConditions = selectedActions
                            .SelectMany(a => new[] { a.Source, a.Target })
                            .SelectMany(x =>
                            {
                                if (x.Conditions != null && x.Conditions.Count > 0)
                                {
                                    x.Condition.Value = targetIndex++;
                                    return x.Conditions!.Select(c => new AbstractConditionLine(0, targetIndex - 1, 0, 0, c));
                                }
                                else
                                {
                                    x.Condition.Value = 0;
                                    return Array.Empty<AbstractConditionLine>();
                                }
                            }).ToList();
                        
                        SmartEvent fakeEvent = new(-1, editorFeatures) {ReadableHint = "", Parent = script};
                        foreach (SmartAction a in selectedActions)
                            fakeEvent.AddAction(a.Copy());

                        var lines = fakeEvent
                            .ToSmartScriptLines(script.EntryOrGuid, script.SourceType, 0);

                        var serialized = JsonConvert.SerializeObject(new ClipboardEvents() { Lines = lines.ToList(), TargetConditions = targetConditions});
                        clipboard.SetText(serialized);
                    }
                }
                else if (AnyConditionSelected)
                {
                    var selectedConditions = Events.SelectMany(e => e.Conditions).Where(e => e.IsSelected).ToList();
                    if (selectedConditions.Count > 0)
                    {
                        SmartEvent fakeEvent = new(-1, editorFeatures) {ReadableHint = "", Parent = script};
                        foreach (SmartCondition c in selectedConditions)
                            fakeEvent.Conditions.Add(c.Copy());

                        var lines = smartScriptExporter.ToDatabaseCompatibleConditions(script, fakeEvent, 0);
                        var serialized = JsonConvert.SerializeObject(new ClipboardEvents(){ Conditions = lines.Select(l => new AbstractConditionLine(l)).ToList() });
                        clipboard.SetText(serialized);
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
                
                var contentString = await clipboard.GetText() ?? "{}";
                ClipboardEvents? content;
                try
                {
                    content = JsonConvert.DeserializeObject<ClipboardEvents>(contentString);
                }
                catch (Exception)
                {
                    return;
                }

                if (content == null)
                    return;
                
                if (content.Lines.Count > 0)
                {
                    if (content.Lines[0].EventType == -1) // actions
                    {
                        Dictionary<int, List<ICondition>>? targetConditions = null;
                        if (content.TargetConditions.Count > 0)
                        {
                            targetConditions = content.TargetConditions.GroupBy(c => c.SourceGroup)
                                .ToDictionary(x => x.Key, x => x.ToList<ICondition>());
                        }
                        
                        int? eventIndex = null;
                        int? actionIndex = null;
                        using (script.BulkEdit("Paste actions"))
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
                            foreach (var smartLine in content.Lines)
                            {
                                var smartAction = script.SafeActionFactory(smartLine);
                                if (smartAction == null)
                                    continue;
                                smartAction.Comment = smartLine.Comment.Contains(" // ")
                                    ? smartLine.Comment.Substring(smartLine.Comment.IndexOf(" // ") + 4).Trim()
                                    : "";
                                Events[eventIndex.Value].Actions.Insert(actionIndex!.Value, smartAction);
                                smartAction.IsSelected = true;
                                actionIndex++;

                                if (smartLine.SourceConditionId > 0 &&
                                    targetConditions != null &&
                                    targetConditions.TryGetValue(smartLine.SourceConditionId, out var sourceConditions))
                                    smartAction.Source.Conditions = sourceConditions;
                                
                                if (smartLine.TargetConditionId > 0 &&
                                    targetConditions != null &&
                                    targetConditions.TryGetValue(smartLine.TargetConditionId, out var sourceConditionsTarget))
                                    smartAction.Target.Conditions = sourceConditionsTarget;
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
                                        index = i + 1;
                                    //else
                                    //    index--;
                                    //Events.RemoveAt(i);
                                    Events[i].IsSelected = false;
                                }
                            }

                            if (!index.HasValue)
                                index = Events.Count;
                            
                            foreach (var e in script.InsertFromClipboard(index.Value, content.Lines, content.Conditions, content.TargetConditions))
                                e.IsSelected = true;
                        }
                    }
                }
                else if (content.Conditions != null && content.Conditions.Count > 0)
                {
                    if (AnyConditionSelected)
                    {
                        int? eventIndex = null;
                        int? conditionIndex = null;
                        using (script!.BulkEdit("Paste conditions"))
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
                            foreach (var smartCondition in content.Conditions.Select(c => script.SafeConditionFactory(c)))
                            {
                                if (smartCondition != null)
                                {
                                    Events[eventIndex.Value].Conditions.Insert(conditionIndex!.Value, smartCondition);
                                    smartCondition.IsSelected = true;
                                    conditionIndex++;   
                                }
                            }
                        }      
                    }
                    else if (AnyEventSelected)
                    {
                        using (script.BulkEdit("Paste conditions"))
                        {
                            var smartEvent = SelectedItem;
                            DeselectAll.Execute();
                            foreach (var smartCondition in content.Conditions.Select(c => script.SafeConditionFactory(c)))
                            {
                                if (smartCondition != null)
                                {
                                    smartEvent?.Conditions.Add(smartCondition);
                                    smartCondition.IsSelected = true;
                                }
                            }
                        }
                    }
                }
                OnPaste?.Invoke();
            });

            int GetNextVisibleEvent(int start, int direction)
            {
                var index = start + direction;
                while (index >= 0 && index < script!.Events.Count)
                {
                    if (Events[index].IsEvent && (!script.TryGetEventGroup(index, out var group, out _) || group.IsExpanded))
                        return index;
                    index += direction;
                }

                return start;
            }
            
            (int eventIndex, int actionIndex) GetNextVisibleAction(int startEvent, int startAction, int direction)
            {
                var eventIndex = startEvent;
                var actionIndex = startAction + direction;
                while (eventIndex >= 0 && eventIndex < script!.Events.Count)
                {
                    if (actionIndex < 0 || actionIndex >= Events[eventIndex].Actions.Count)
                    {
                        var nextEventIndex = GetNextVisibleEvent(eventIndex, direction);
                        if (nextEventIndex == eventIndex)
                            return (startEvent, startAction);
                        eventIndex = nextEventIndex;
                        actionIndex = direction > 0 ? 0 : Events[eventIndex].Actions.Count - 1;
                    }
                    else if (hideComments && Events[eventIndex].Actions[actionIndex].Id == SmartConstants.ActionComment)
                    {
                        actionIndex += direction;
                    }
                    else
                    {
                        return (eventIndex, actionIndex);
                    }
                }
                return (-1, -1);
            }

            SmartGroup? GetNextGroup(int start, int direction)
            {
                var index = start + direction;
                if (direction > 0)
                {
                    while (script!.TryGetEvent(index, out var e))
                    {
                        if (e.IsBeginGroup)
                            return new SmartGroup(e);
                        index += direction;
                    }

                    return null;
                }
                else
                {
                    if (script!.TryGetEventGroup(index, out var group, out _))
                    {
                        while (script!.TryGetEvent(index, out var e) && !e.IsBeginGroup)
                            index += direction; // find begin of this group
                        index += direction;
                    }

                    while (script!.TryGetEvent(index, out var e))
                    {
                        if (e.IsBeginGroup)
                            return new SmartGroup(e);
                        index += direction;
                    }
                    return null;
                }
            }
            
            void SelectionUpDown(bool addToSelection, int diff)
            {
                if (AnyGlobalVariableSelected)
                {
                    int selectedGlobalVariableIndex = Math.Clamp(FirstSelectedGlobalVariableIndex + diff, 0, script.GlobalVariables.Count - 1);
                    if (addToSelection)
                    {
                        script.GlobalVariables[selectedGlobalVariableIndex].IsSelected = true;
                    }
                    else
                    {
                        if (FirstSelectedGlobalVariableIndex + diff == script.GlobalVariables.Count && Events.Count > 0)
                        {
                            DeselectAll.Execute();
                            Events[0].IsSelected = true;
                        }
                        else
                        {
                            if (addToSelection)
                                DeselectAllButGlobalVariables.Execute();
                            else
                                DeselectAll.Execute();
                            script.GlobalVariables[selectedGlobalVariableIndex].IsSelected = true;
                        }
                    }
                }
                else if (AnyGroupSelected)
                {
                    SmartGroup? nextGroup = diff < 0 ? GetNextGroup(FirstSelectedGroupIndex, -1) : GetNextGroup(LastSelectedGroupIndex, 1);
                    if (nextGroup != null)
                    {
                        DeselectAll.Execute();
                        nextGroup.IsSelected = true;
                    }
                }
                else if (AnyEventSelected)
                {
                    int selectedEventIndex = diff < 0 ? GetNextVisibleEvent(FirstSelectedEventIndex, -1) : GetNextVisibleEvent(LastSelectedEventIndex, 1);
                    if (addToSelection)
                    {
                        Events[selectedEventIndex].IsSelected = true;
                    }
                    else
                    {
                        if (FirstSelectedEventIndex + diff == -1 && script.GlobalVariables.Count > 0)
                        {
                            DeselectAll.Execute();
                            script.GlobalVariables[^1].IsSelected = true;
                        }
                        else
                        {
                            DeselectAll.Execute();
                            Events[selectedEventIndex].IsSelected = true;
                        }
                    }
                }
                else if (AnyActionSelected)
                {
                    (int nextEventIndex, int nextActionIndex) = GetNextVisibleAction(
                        diff < 0 ? FirstSelectedActionIndex.eventIndex : LastSelectedActionIndex.eventIndex,
                        diff < 0 ? FirstSelectedActionIndex.actionIndex : LastSelectedActionIndex.actionIndex,
                        diff);
                    
                    if (nextActionIndex != -1 && nextEventIndex != -1)
                    {
                        if (!addToSelection)
                            DeselectAll.Execute();
                        else
                            DeselectAllButActions.Execute();
                        Events[nextEventIndex].Actions[nextActionIndex].IsSelected = true;
                    }
                }
                else if (AnyConditionSelected)
                {            
                    int nextConditionIndex = diff < 0 ? FirstSelectedConditionIndex.conditionIndex + diff : LastSelectedConditionIndex.conditionIndex + diff;
                    int nextEventIndex = diff < 0 ? FirstSelectedConditionIndex.eventIndex : LastSelectedConditionIndex.eventIndex;
                    
                    while (nextConditionIndex == -1 || nextConditionIndex >= Events[nextEventIndex].Conditions.Count)
                    {
                        nextEventIndex = GetNextVisibleEvent(nextEventIndex, diff);
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
                        if (!addToSelection)
                            DeselectAll.Execute();
                        else
                            DeselectAllButConditions.Execute();
                        Events[nextEventIndex].Conditions[nextConditionIndex].IsSelected = true;
                    }
                }
                else
                {
                    if (Events.Count > 0) Events[diff > 0 ? 0 : Events.Count - 1].IsSelected = true;
                }
            }

            SelectionUp = new DelegateCommand<bool?>(addToSelection => SelectionUpDown(addToSelection ?? false, -1));
            SelectionDown = new DelegateCommand<bool?>(addToSelection => SelectionUpDown(addToSelection ?? false, 1));
            SelectionLeft = new DelegateCommand(() =>
            {
                if (!AnyEventSelected && AnyActionSelected)
                {
                    (int eventIndex, int actionIndex) actionEventIndex = FirstSelectedActionIndex;
                    DeselectAll.Execute();
                    Events[actionEventIndex.eventIndex].IsSelected = true;
                }
                else if (!AnyGroupSelected && AnyEventSelected)
                {
                    var eventIndex = FirstSelectedEventIndex;
                    if (script.TryGetEventGroup(Events[eventIndex], out var group, out _))
                    {
                        DeselectAll.Execute();
                        group.IsSelected = true;
                    }
                }
                else if (!AnyEventSelected && !AnyActionSelected)
                    SelectionUpDown(false, -1);
            });
            SelectionRight = new DelegateCommand(() =>
            {
                if (!AnyEventSelected)
                    SelectionUpDown(false, -1);
                else if (AnyGroupSelected)
                {
                    var eventIndex = FirstSelectedEventIndex;
                    DeselectAll.Execute();
                    if (script.TryGetEvent(eventIndex, out var e))
                        e.IsSelected = true;
                }
                else if (AnyEventSelected)
                {
                    int eventIndex = FirstSelectedEventIndex;
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

            RemoveAllComments = new DelegateCommand(() =>
            {
                using var _ = script.BulkEdit("Remove all comments");
                foreach (SmartEvent e in Events)
                {
                    for (int i = e.Actions.Count - 1; i >= 0; --i)
                    {
                        if (e.Actions[i].Id == SmartConstants.ActionComment)
                            e.Actions.RemoveAt(i);
                    }
                }
            });

            async Task NewAction(bool below)
            {
                var index = below ? LastSelectedActionIndex : FirstSelectedActionIndex;
                if (index.actionIndex == -1 || index.eventIndex == -1)
                {
                    index = (below ? LastSelectedEventIndex : FirstSelectedEventIndex, 0);
                    if (index.eventIndex == -1)
                        return;
                }

                if (below)
                    index = (index.eventIndex, index.actionIndex + 1);

                await AddActionCommand(new NewActionViewModel() { Event = Events[index.eventIndex], InsertIndex = index.actionIndex });
            }
            
            async Task NewEvent(bool below)
            {
                var index = below ? LastSelectedEventIndex : FirstSelectedEventIndex;
                if (index == -1)
                    index = Events.Count - 1;

                if (below)
                    index++;

                await AddEventCommand(index);
            }

            async Task NewGroup(bool below)
            {
                var index = below ? LastSelectedEventIndex : FirstSelectedEventIndex;
                if (index == -1)
                    index = Events.Count - 1;

                if (below)
                    index++;

                var fakeGroup = new SmartGroup(SmartEvent.NewBeginGroup());
                if (await windowManager.ShowDialog(new SmartGroupEditViewModel(fakeGroup)))
                {
                    using var _ = script.BulkEdit("Insert group");
                    var group = script.InsertGroupBegin(ref index, fakeGroup.Header, fakeGroup.Description);
                    script.InsertGroupEnd(index + 1);
                }
            }

            long GetNextLinkId()
            {
                long max = 1;
                foreach (SmartEvent e in Events)
                {
                    if (e.Id == SmartConstants.EventLink)
                        max = Math.Max(max, e.GetParameter(0).Value + 1);
                    foreach (SmartAction a in e.Actions)
                    {
                        if (a.Id == SmartConstants.ActionLink)
                        {
                            max = Math.Max(max, a.GetParameter(0).Value + 1);
                        }
                    }
                }
                return max;
            }

            AddLinkCommand = new DelegateCommand(() =>
            {
                using (script.BulkEdit("Insert a link"))
                {
                    int eventIndex;
                    int actionIndex;
                    if (AnyEventSelected)
                    {
                        eventIndex = FirstSelectedEventIndex;
                        actionIndex = Events[eventIndex].Actions.Count - 1;
                    }
                    else if (AnyActionSelected)
                        (eventIndex, actionIndex) = FirstSelectedActionIndex;
                    else 
                        return;
                    
                    var @event = smartFactory.EventFactory(SmartConstants.EventLink);
                    var action = smartFactory.ActionFactory(SmartConstants.ActionLink, null, null);
                    @event.GetParameter(0).Value = action.GetParameter(0).Value = GetNextLinkId();
                    Events[eventIndex].Actions.Insert(actionIndex + 1, action);
                    Events.Insert(eventIndex + 1, @event);
                    for (int i = Events[eventIndex].Actions.Count - 1; i > actionIndex + 1; --i)
                    {
                        var a = Events[eventIndex].Actions[i];
                        Events[eventIndex].Actions.RemoveAt(Events[eventIndex].Actions.Count - 1);
                        @event.Actions.Insert(0, a);
                    }
                }
            }, () => AnyActionSelected || AnyEventSelected).ObservesProperty(() => AnyActionSelected).ObservesProperty(() => AnyEventSelected);
            
            NewActionAboveCommand = new AsyncCommand(() => NewAction(false));
            NewActionBelowCommand = new AsyncCommand(() => NewAction(true));
            NewEventAboveCommand = new AsyncCommand(() => NewEvent(false));
            NewEventBelowCommand = new AsyncCommand(() => NewEvent(true));
            NewGroupAboveCommand = new AsyncCommand(() => NewGroup(false));
            NewGroupBelowCommand = new AsyncCommand(() => NewGroup(true));

            EditConditionsCommand = new AsyncCommand(() => ExternalConditionEdit(Events.Where(e => e.IsSelected).ToList()),
                _ => AnyEventSelected);
            
            DismissNotification = new DelegateCommand<SmartExtensionNotification>(notification =>
            {
                Notifications.Remove(notification);
                notification.Dispose();
            });

            SaveDefaultScaleCommand = new DelegateCommand(() =>
            {
                preferences.DefaultScale = scale;
                preferences.Apply();
            });

            History.PropertyChanged += (_, _) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };

            AutoDispose(problems.Subscribe(foundProblems =>
            {
                var lines = problematicLines ?? new();
                lines.Clear();
                foreach (var p in foundProblems)
                {
                    lines[p.Line] = p.Severity;
                }

                ProblematicLines = null;
                ProblematicLines = lines;
            }));

            SetSolutionItem(item);
        }

        public Task<IQuery> GenerateQuery()
        {
            return Task.FromResult(smartScriptExporter.GenerateSql(item, script));
        }

        public string Name => itemNameRegistry.GetName(item);

        public ObservableCollection<SmartEvent> Events => script.Events;

        public SmartEvent? SelectedItem => Events.FirstOrDefault(ev => ev.IsSelected);

        public DelegateCommand EditEvent { get; set; }

        public DelegateCommand DeselectAll { get; set; }
        public DelegateCommand DeselectAllButGlobalVariables { get; set; }
        public DelegateCommand DeselectAllButConditions { get; set; }
        public DelegateCommand DeselectAllButActions { get; set; }
        public DelegateCommand DeselectAllButEvents { get; set; }
        public DelegateCommand DeselectAllButGroups { get; set; }
        public DelegateCommand RemoveAllComments { get; }
        
        public AsyncAutoCommand<object> DirectEditParameter { get; }
        public AsyncAutoCommand<object> DirectOpenParameter { get; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropConditions { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropActions { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropItems { get; set; }
        public DelegateCommand<DropActionsConditionsArgs> OnDropGroups { get; set; }
        
        public AsyncAutoCommand<NewActionViewModel> AddAction { get; set; }
        public AsyncAutoCommand<NewConditionViewModel> AddCondition { get; set; }

        public DelegateCommand<SmartAction> DeleteAction { get; set; }

        public AsyncAutoCommand EditAction { get; set; }
        public AsyncAutoCommand EditCondition { get; }
        public AsyncAutoCommand AddEvent { get; set; }
        public AsyncAutoCommand DefineGlobalVariable { get; set; }

        public DelegateCommand UndoCommand { get; set; }
        public DelegateCommand RedoCommand { get; set; }

        public AsyncCommand CopyCommand { get; set; }
        public AsyncCommand CutCommand { get; set; }
        public AsyncCommand PasteCommand { get; set; }
        public event Action? OnPaste;
        public AsyncAutoCommand SaveCommand { get; set; }
        public DelegateCommand DeleteSelected { get; set; }
        public DelegateCommand EditSelected { get; set; }

        public AsyncCommand NewActionAboveCommand { get; set; }
        public AsyncCommand NewActionBelowCommand { get; set; }
        public AsyncCommand NewEventAboveCommand { get; set; }
        public AsyncCommand NewEventBelowCommand { get; set; }
        public AsyncCommand NewGroupAboveCommand { get; set; }
        public AsyncCommand NewGroupBelowCommand { get; set; }
        public AsyncCommand EditConditionsCommand { get; set; }
        public DelegateCommand AddLinkCommand { get; set; }

        public DelegateCommand<bool?> SelectionUp { get; set; }
        public DelegateCommand<bool?> SelectionDown { get; set; }
        public DelegateCommand SelectionRight { get; set; }
        public DelegateCommand SelectionLeft { get; set; }
        public DelegateCommand SelectAll { get; set; }

        public bool AnySelected => AnyGroupSelected || AnyEventSelected || AnyActionSelected;
        private bool AnyGlobalVariableSelected => script.GlobalVariables.Any(e => e.IsSelected);
        private bool MultipleGlobalVariablesSelected => script.GlobalVariables.Count(e => e.IsSelected) >= 2;
        public bool AnyEventSelected => Events.Any(e => e.IsEvent && e.IsSelected);
        public bool AnyGroupSelected => Events.Any(e => e.IsBeginGroup && e.IsSelected);
        private bool MultipleGroupsSelected => Events.Count(e => e.IsSelected && e.IsBeginGroup) >= 2;
        private bool MultipleEventsSelected => Events.Count(e => e.IsSelected && e.IsEvent) >= 2;
        private bool MultipleActionsSelected => Events.SelectMany(e => e.Actions).Count(a => a.IsSelected) >= 2;
        public bool AnyActionSelected => Events.SelectMany(e => e.Actions).Any(a => a.IsSelected);
        private bool MultipleConditionsSelected => Events.SelectMany(e => e.Conditions).Count(a => a.IsSelected) >= 2;
        private bool AnyConditionSelected => Events.SelectMany(e => e.Conditions).Any(a => a.IsSelected);

        private int FirstSelectedGlobalVariableIndex =>
            script.GlobalVariables.Select((e, index) => (e.IsSelected, index)).Where(p => p.IsSelected).Select(p => p.index).FirstOrDefault();

        private int FirstSelectedEventIndex =>
            Events.Select((e, index) => (e.IsSelected && e.IsEvent, index)).Where(p => p.Item1).Select(p => p.index).FirstOrDefault(-1);

        private int LastSelectedEventIndex =>
            Events.Select((e, index) => (e.IsSelected && e.IsEvent, index)).Where(p => p.Item1).Select(p => p.index).LastOrDefault(-1);

        private int FirstSelectedGroupIndex =>
            Events.Select((e, index) => (e.IsSelected && e.IsBeginGroup, index)).Where(p => p.Item1).Select(p => p.index).FirstOrDefault(-1);

        private int LastSelectedGroupIndex =>
            Events.Select((e, index) => (e.IsSelected && e.IsBeginGroup, index)).Where(p => p.Item1).Select(p => p.index).LastOrDefault(-1);

        private (int eventIndex, int actionIndex) FirstSelectedActionIndex =>
            Events.SelectMany((e, eventIndex) => e.Actions.Select((a, actionIndex) => (a.IsSelected, eventIndex, actionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.actionIndex))
                .FirstOrDefault((-1, -1));

        private (int eventIndex, int actionIndex) LastSelectedActionIndex =>
            Events.SelectMany((e, eventIndex) => e.Actions.Select((a, actionIndex) => (a.IsSelected, eventIndex, actionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.actionIndex))
                .LastOrDefault((-1, -1));

        private (int eventIndex, int conditionIndex) FirstSelectedConditionIndex =>
            Events.SelectMany((e, eventIndex) => e.Conditions.Select((a, conditionIndex) => (a.IsSelected, eventIndex, conditionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.conditionIndex))
                .FirstOrDefault((-1, -1));
                
        private (int eventIndex, int conditionIndex) LastSelectedConditionIndex =>
            Events.SelectMany((e, eventIndex) => e.Conditions.Select((a, conditionIndex) => (a.IsSelected, eventIndex, conditionIndex)))
                .Where(p => p.IsSelected)
                .Select(p => (p.eventIndex, p.conditionIndex))
                .LastOrDefault((-1, -1));

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
        public IAsyncCommand? CloseCommand { get; set; }
        
        private void SetSolutionItem(ISmartScriptSolutionItem newItem)
        {
            Debug.Assert(this.item == null);
            this.item = newItem;
            Title = newItem.SmartType == SmartScriptType.TimedActionList ?
                itemNameRegistry.GetName(newItem) :
                itemNameRegistry.GetName(newItem) + $" ({newItem.Entry})";
            
            Script = new SmartScript(this.item, smartFactory, smartDataManager, messageBoxService, editorFeatures, smartScriptImporter);

            bool updateInspections = false;
            AutoDispose(mainThread.StartTimer(() =>
            {
                if (updateInspections)
                {
                    outlinerService.Process(outlinerData, script);
                    outlinerModel.Publish(outlinerData);
                    
                    problems.Value = inspectorService.GenerateInspections(script);
                    updateInspections = false;
                }

                return true;
            }, TimeSpan.FromMilliseconds(1200)));
            
            script.EventChanged += (e, a, _) =>
            {
                updateInspections = true;
                if (e != null || a?.Parent != null)
                    RecolorEvent((e ?? a?.Parent)!);
            };
            
            TeachingTips = AutoDispose(new SmartTeachingTips(teachingTipService, this, Script));
            Script.ScriptSelectedChanged += EventChildrenSelectionChanged;
            
            taskRunner.ScheduleTask($"Loading script {Title}", AsyncLoad);
        }

        private ParametersEditViewModel? currentlyPreviewedViewModel;

        private void EventChildrenSelectionChanged()
        {
            RaisePropertyChanged(nameof(AnySelected));
            RaisePropertyChanged(nameof(AnyActionSelected));
            RaisePropertyChanged(nameof(AnyEventSelected));
            EditConditionsCommand.RaiseCanExecuteChanged();
            if (!smartEditorViewModel.IsOpened)
                return;
                
            currentlyPreviewedViewModel?.Dispose();
            currentlyPreviewedViewModel = null;
            
            if (AnyEventSelected)
            {
                currentlyPreviewedViewModel = EventEditViewModel(Events.Where(e => e.IsSelected && e.IsEvent).ToList(), true);
            }
            else if (AnyConditionSelected)
            {
                currentlyPreviewedViewModel = ConditionEditViewModel(Events.SelectMany(e => e.Conditions).Where(a => a.IsSelected).ToList(), true);
            }
            else if (AnyActionSelected)
            {
                currentlyPreviewedViewModel = ActionEditViewModel(Events.SelectMany(e => e.Actions).Where(a => a.IsSelected).ToList(), true);
            }
            
            if (currentlyPreviewedViewModel != null)
                currentlyPreviewedViewModel.ShowCloseButtons = false;
                
            smartEditorViewModel.ShowEditor(Script, currentlyPreviewedViewModel);
        }

        private async Task AsyncLoad()
        {
            await editorExtension.BeforeLoad(this, item);
            var lines = (await smartScriptDatabase.GetScriptFor(item.Entry, item.SmartType)).ToList();
            var conditions = smartScriptDatabase.GetConditionsForScript(item.Entry, item.SmartType).ToList();
            var targetSourceConditions = smartScriptDatabase.GetConditionsForSourceTarget(item.Entry, item.SmartType).ToList();
            await smartScriptImporter.Import(script, false, lines, conditions, targetSourceConditions);
            IsLoading = false;
            History.AddHandler(new SaiHistoryHandler(script, smartFactory));
            TeachingTips.Start();
            problems.Value = inspectorService.GenerateInspections(script);
            script.OriginalLines.AddRange(lines);
        }
        
        private async Task SaveAllToDb()
        {
            statusbar.PublishNotification(new PlainNotification(NotificationType.Info, "Saving to database"));

            var query = smartScriptExporter.GenerateSql(item, script);

            await mySqlExecutor.ExecuteSql(query);

            statusbar.PublishNotification(new PlainNotification(NotificationType.Success, "Saved to database"));
            
            History.MarkAsSaved();
        }

        private void DeleteActionCommand(SmartAction obj)
        {
            obj.Parent?.Actions.Remove(obj);
        }

        private async Task AddActionCommand(NewActionViewModel obj)
        {
            SmartEvent? e = obj.Event;
            if (e == null)
                return;

            SmartAction? action = null;
            
            if (preferences.AddingBehaviour == AddingElementBehaviour.Wizard)
            {
                action = await AddActionWizard(e);
            }
            else
            {
                var source = smartFactory.SourceFactory(SmartConstants.SourceSelf);
                var target = smartFactory.TargetFactory(script.SourceType == SmartScriptType.Creature ? SmartConstants.TargetSelf : SmartConstants.TargetNone);
                action = smartFactory.ActionFactory(script.SourceType == SmartScriptType.Creature ? SmartConstants.ActionTalk : SmartConstants.ActionNone, source, target);
                action.Parent = e; // <-- set the parent already, so that the edit window has the correct parent
                if (preferences.AddingBehaviour == AddingElementBehaviour.JustAdd)
                {
                    // empty
                }
                else if (preferences.AddingBehaviour == AddingElementBehaviour.DirectlyOpenDialog)
                {
                    if (!await EditActionCommand(new[] { action }))
                        return;
                }
                else
                    throw new ArgumentOutOfRangeException("Unknown adding behaviour " + preferences.AddingBehaviour);
            }
            
            if (action != null)
            {
                DeselectAll.Execute();
                if (!obj.InsertIndex.HasValue || obj.InsertIndex < 0 || obj.InsertIndex > e.Actions.Count)
                    e.Actions.Add(action);
                else
                    e.Actions.Insert(obj.InsertIndex.Value, action);
                action.IsSelected = true;
            }
        }

        private async Task<SmartAction?> AddActionWizard(SmartEvent e)
        {
            var sourcePick = await ShowSourcePicker(e);

            if (!sourcePick.HasValue)
                return null;

            int sourceId = sourcePick.Value.Item2 ? SmartConstants.SourceStoredObject : sourcePick.Value.Item1;
            
            int? actionId = await ShowActionPicker(e, sourceId);

            if (!actionId.HasValue)
                return null;

            SmartGenericJsonData actionData = smartDataManager.GetRawData(SmartType.SmartAction, actionId.Value);

            SmartTarget? target;

            if (!actionData.TargetIsSource && !actionData.DoNotProposeTarget && actionData.TargetTypes != SmartSourceTargetType.None)
            {
                var targetPick = await ShowTargetPicker(e, actionData);

                if (!targetPick.HasValue)
                    return null;
                
                int targetId = targetPick.Value.Item2 ? SmartConstants.SourceStoredObject : targetPick.Value.Item1;

                target = smartFactory.TargetFactory(targetId);
                if (targetPick.Value.Item2)
                    target.GetParameter(0).Value = targetPick.Value.Item1;
            }
            else
            {
                var usesTarget = actionData.TargetTypes != SmartSourceTargetType.None && actionData.DoNotProposeTarget;
                target = smartFactory.TargetFactory(usesTarget ? SmartConstants.TargetSelf : SmartConstants.TargetNone);
            }
            
            SmartSource source = smartFactory.SourceFactory(sourceId);
            if (sourcePick.Value.Item2)
                source.GetParameter(0).Value = sourcePick.Value.Item1;

            SmartAction ev = smartFactory.ActionFactory(actionId.Value, source, target);
            SmartGenericJsonData targetData = smartDataManager.GetRawData(SmartType.SmartTarget, target.Id);
            ev.Parent = e;
            bool anyUsed = ev.GetParameter(0).IsUsed ||
                              source.GetParameter(0).IsUsed ||
                              target.GetParameter(0).IsUsed ||
                              actionData.CommentField != null ||
                              targetData.UsesTargetPosition;
            if (!anyUsed || await EditActionCommand(new[] { ev }))
            {
                return ev;
            }

            return null;
        }

        private async Task ExternalConditionEdit(List<SmartEvent> events)
        {
            if (events.Count == 0)
                return;
            
            var conditions = smartScriptExporter.ToDatabaseCompatibleConditions(script, events[0]);
            var result = await conditionEditService.EditConditions(SmartConstants.ConditionSourceSmartScript, conditions);
            if (result == null)
                return;

            var parsed =  smartScriptImporter.ImportConditions(script, result.Select(l => new AbstractConditionLine(0, 0, 0, 0, l)).ToList());
            using var _ = script.BulkEdit("Edit conditions");

            foreach (var e in events)
            {
                e.Conditions.RemoveAll();
                if (parsed.TryGetValue(0, out var flatList))
                {
                    foreach (var cond in flatList)
                        e.Conditions.Add(cond.Copy());
                }
            }
        }
        
        private async Task AddConditionCommand(NewConditionViewModel obj)
        {
            SmartEvent? e = obj.Event;
            if (e == null)
                return;

            if (editorFeatures.UseExternalConditionsEditor)
            {
                await ExternalConditionEdit(new List<SmartEvent>(){e});
            }
            else
            {
                int? conditionId = await ShowConditionPicker();
            
                if (!conditionId.HasValue)
                    return;

                var conditionData = conditionDataManager.GetConditionData(conditionId.Value);

                SmartCondition ev = smartFactory.ConditionFactory(conditionId.Value);
                ev.Parent = e;
            
                if ((conditionData.Parameters?.Count ?? 0) == 0 || await EditConditionCommand(new []{ev}))
                    e.Conditions.Add(ev);   
            }
        }

        private async Task<int?> ShowConditionPicker()
        {
            var result = await smartTypeListProvider.Get(SmartType.SmartCondition, _ => true);
            if (result.HasValue)
                return result.Value.Item1;
            return null;
        }
        
        private async Task<int?> ShowEventPicker()
        {
            var result = await smartTypeListProvider.Get(SmartType.SmartEvent,
                data => data.UsableWithScriptTypes == null || data.UsableWithScriptTypes.Contains(script.SourceType));
            if (result.HasValue)
                return result.Value.Item1;
            return null;
        }

        private Task<(int, bool)?> ShowTargetPicker(SmartEvent? parentEvent, SmartGenericJsonData? actionData)
        {           
            var eventSupportsActionInvoker =
                parentEvent?.Parent == null || parentEvent.Parent.GetEventData(parentEvent).Invoker != null;
            return smartTypeListProvider.Get(SmartType.SmartTarget,
                data =>
                {
                    if (data.UsableWithEventTypes != null && parentEvent != null && !data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return (eventSupportsActionInvoker || !data.IsInvoker) &&
                           (data.UsableWithScriptTypes == null ||
                            data.UsableWithScriptTypes.Contains(script.SourceType)) &&
                           (!actionData.HasValue || actionData.Value.TargetTypes == SmartSourceTargetType.None || (data.Types(script.SourceType) != SmartSourceTargetType.None && (actionData.Value.TargetTypes & data.Types(script.SourceType)) != 0));
                }, BuildStoredObjectsList());
        }

        private Task<(int, bool)?> ShowSourcePicker(SmartEvent? parentEvent, SmartGenericJsonData? actionData = null)
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

                    if (actionData.HasValue && !IsSourceCompatibleWithAction(data, actionData.Value))
                        return false;

                    if (data.UsableWithEventTypes != null && parentEvent != null && !data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return data.UsableWithScriptTypes == null || data.UsableWithScriptTypes.Contains(script.SourceType);
                }, BuildStoredObjectsList());
        }

        private List<(int, string)>? BuildStoredObjectsList()
        {
            if (script.GlobalVariables.Count == 0)
                return null;
            
            return script.GlobalVariables
                .Where(gv => gv.VariableType == GlobalVariableType.StoredTarget && gv.Name != "")
                .Select(gv => ((int) gv.Key, gv.Name))
                .ToList();
        }

        private bool IsSourceCompatibleWithAction(SmartGenericJsonData sourceData, SmartGenericJsonData actionData)
        {
            if (actionData.ImplicitSource != null)
            {
                var data = smartDataManager.GetDataByName(SmartType.SmartTarget, actionData.ImplicitSource);
                var actionImplicitSource = data.Id;

                if (sourceData.Id == actionImplicitSource)
                    return true;

                // kinda hack to show actions with NONE source with user pick SELF source
                // because it is natural for users to use SELF source for those actions
                return actionImplicitSource == SmartConstants.SourceNone && (sourceData.Id == SmartConstants.SourceSelf || sourceData.NameReadable == "Self");
            }
            else
            {
                SmartSourceTargetType possibleSourcesOfAction = actionData.TargetIsSource ? actionData.TargetTypes : actionData.Sources;
                var possibleSourcesOfSource = sourceData.Types(script.SourceType);

                if ((sourceData.Id == SmartConstants.SourceSelf || sourceData.Id == SmartConstants.SourceNone)
                    && possibleSourcesOfSource == SmartSourceTargetType.None)
                    return true;
                
                if (sourceData.Id == SmartConstants.SourceSelf && script.SourceType == SmartScriptType.Creature)
                    return possibleSourcesOfAction.HasFlagFast(SmartSourceTargetType.Creature);

                if (sourceData.Id == SmartConstants.SourceSelf && script.SourceType == SmartScriptType.GameObject)
                    return possibleSourcesOfAction.HasFlagFast(SmartSourceTargetType.GameObject);

                return (possibleSourcesOfAction & possibleSourcesOfSource) != 0;
            }
        }
        
        private async Task<int?> ShowActionPicker(SmartEvent? parentEvent, int? sourceId, bool showCommentMetaAction = true)
        {
            SmartGenericJsonData? sourceData = sourceId.HasValue ? smartDataManager.GetRawData(SmartType.SmartTarget, sourceId.Value) : null;
            var result = await smartTypeListProvider.Get(SmartType.SmartAction,
                data =>
                {
                    if (data.UsableWithEventTypes != null && parentEvent != null && !data.UsableWithEventTypes.Contains(parentEvent.Id))
                        return false;
                    
                    return (data.UsableWithScriptTypes == null ||
                            data.UsableWithScriptTypes.Contains(script.SourceType)) &&
                           (!sourceData.HasValue || IsSourceCompatibleWithAction(sourceData.Value, data)) &&
                           (showCommentMetaAction || data.Id != SmartConstants.ActionComment);
                });
            if (result.HasValue)
                return result.Value.Item1;
            return null;
        }

        private async Task AddEventCommand(int? insertIndex)
        {
            int? id;
            if (preferences.AddingBehaviour == AddingElementBehaviour.Wizard)
            {
                id = await ShowEventPicker();
            }
            else
            {
                id = SmartConstants.EventUpdateInCombat;
            }
            
            if (id.HasValue)
            {
                DeselectAll.Execute();
                SmartEvent ev = smartFactory.EventFactory(id.Value);
                ev.Parent = script;
                if (preferences.AddingBehaviour == AddingElementBehaviour.JustAdd ||
                    !ev.GetParameter(0).IsUsed ||
                    await EditEventCommand(new[] { ev }))
                {
                    if (!insertIndex.HasValue || insertIndex < 0 || insertIndex > script.Events.Count)
                    {
                        script.Events.Add(ev);
                    }
                    else
                    {
                        script.Events.Insert(insertIndex.Value, ev);
                    }
                    ev.IsSelected = true;

                    if (preferences.InsertActionOnEventInsert)
                    {
                        await NewActionAboveCommand.ExecuteAsync();
                    }
                }
            }
        }

        private ParametersEditViewModel ActionEditViewModel(IReadOnlyList<SmartAction> originalActions, bool editOriginal = false)
        {
            bool isComment = originalActions[0].Id == SmartConstants.ActionComment;
            IReadOnlyList<SmartAction> actionsToEdit = editOriginal ? originalActions : originalActions.Select(a => a.Copy()).ToList();
            if (!editOriginal)
                actionsToEdit.Each(a => a.Parent = originalActions[0].Parent);

            var bulkEdit = new ScriptBulkEdit(script);
            SmartEditableGroup editableGroup = new SmartEditableGroup(bulkEdit);
            
            var sourceParameters = editableGroup.Add("Source", actionsToEdit.Select(a => a.Source).ToList(), originalActions.Select(a => a.Source).ToList());
            var firstSourceParameter = editableGroup.Parameters[^actionsToEdit[0].Source.ParametersCount];
            var actionParameters = editableGroup.Add("Action", actionsToEdit, originalActions);
            var targetParameters = editableGroup.Add("Target", actionsToEdit.Select(a => a.Target).ToList(), originalActions.Select(a => a.Target).ToList());
            var firstTargetParameter = editableGroup.Parameters[^actionsToEdit[0].Target.ParametersCount];
            
            editableGroup.Add("Comment", actionsToEdit, originalActions, a => a.CommentParameter);
            
            MultiPropertyValueHolder<int, SmartSource> sourceType = new MultiPropertyValueHolder<int, SmartSource>(0,
                actionsToEdit.Select(a => a.Source).ToList(),
                originalActions.Select(a => a.Source).ToList(),
                e => e.Id,
                (e, id) =>
                {
                    smartFactory.UpdateSource(e, id);
                    FillNonzeroWithDefaults(SmartType.SmartSource, id, sourceParameters);
                }, bulkEdit);
            MultiPropertyValueHolder<int, SmartTarget> targetType = new MultiPropertyValueHolder<int, SmartTarget>(0,
                actionsToEdit.Select(a => a.Target).ToList(),
                originalActions.Select(a => a.Target).ToList(),
                e => e.Id,
                (e, id) =>
                {
                    smartFactory.UpdateTarget(e, id);
                    FillNonzeroWithDefaults(SmartType.SmartTarget, id, targetParameters);
                }, bulkEdit);
            MultiPropertyValueHolder<int, SmartAction> actionType = new MultiPropertyValueHolder<int, SmartAction>(0,
                actionsToEdit,
                originalActions,
                e => e.Id,
                (e, id) =>
                {
                    smartFactory.UpdateAction(e, id);
                    FillNonzeroWithDefaults(SmartType.SmartAction, id, actionParameters);
                    var actionData = smartDataManager.GetRawData(SmartType.SmartAction, id);
                    if (actionData.TargetTypes != SmartSourceTargetType.None &&
                        !targetType.HoldsMultipleValues &&
                        targetType.Value == SmartConstants.TargetNone &&
                        script.SourceType is SmartScriptType.Creature or SmartScriptType.GameObject or SmartScriptType.Template)
                        targetType.Value = SmartConstants.TargetSelf;
                }, bulkEdit);
            
            bool modifiedSourceConditions = false;
            bool modifiedTargetConditions = false;

            Func<Task>? selectSourceTypeCommand = null;
            Func<Task>? selectActionTypeCommand = null;
            Func<Task>? selectTargetTypeCommand = null;
            if (!isComment)
            {
                var actionDataObservable = actionType.ToObservable(e => e.Value).Select(id => actionType.HoldsMultipleValues ? (SmartGenericJsonData?)null : smartDataManager.GetRawData(SmartType.SmartAction, id));
                var sourceDataObservable = sourceType.ToObservable(e => e.Value).Select(id => sourceType.HoldsMultipleValues ? (SmartGenericJsonData?)null : smartDataManager.GetRawData(SmartType.SmartSource, id));
                var targetDataObservable = targetType.ToObservable(e => e.Value).Select(id => targetType.HoldsMultipleValues ? (SmartGenericJsonData?)null : smartDataManager.GetRawData(SmartType.SmartTarget, id));

                var selectSource = new EditableActionData("Type", "Source", async () =>
                {
                    var newSourceIndex = await ShowSourcePicker(actionsToEdit[0].Parent);
                    if (!newSourceIndex.HasValue)
                        return;

                    var newId = newSourceIndex.Value.Item2
                        ? SmartConstants.SourceStoredObject
                        : newSourceIndex.Value.Item1;

                    var newSourceData = smartDataManager.GetRawData(SmartType.SmartTarget, newId);
                    var actionData = actionType.HoldsMultipleValues
                        ? (SmartGenericJsonData?)null
                        : smartDataManager.GetRawData(SmartType.SmartAction, actionType.Value);
                    if (actionData.HasValue && !IsSourceCompatibleWithAction(newSourceData, actionData.Value))
                    {
                        var sourceData = smartDataManager.GetRawData(SmartType.SmartSource, newId);
                        var dialog = new MessageBoxFactory<bool>()
                            .SetTitle("Incorrect source for chosen action")
                            .SetMainInstruction(
                                $"The source you have chosen ({sourceData.NameReadable}) is not supported with action {actionData.Value.NameReadable}");
                        if (string.IsNullOrEmpty(actionData.Value.ImplicitSource))
                            dialog.SetContent(
                                $"Selected source can be one of: {sourceData.RawTypes}. However, current action requires one of: {actionData.Value.TargetTypes}");
                        else
                            dialog.SetContent(
                                $"In TrinityCore some actions do not support some sources, this is one of the case. Following action will ignore chosen source and will use source: {actionData.Value.ImplicitSource}");
                        await messageBoxService.ShowDialog(dialog.SetIcon(MessageBoxIcon.Information).Build());
                    }

                    sourceType.Value = newId;
                    if (newSourceIndex.Value.Item2)
                        firstSourceParameter.parameter.Value = newSourceIndex.Value.Item1;
                }, sourceDataObservable.Select(a => a?.NameReadable ?? "---"), null, sourceType);

                var selectAction = new EditableActionData("Type", "Action", async () =>
                {
                    int? newActionIndex = await ShowActionPicker(actionsToEdit[0].Parent, null,
                        //sourceType.HoldsMultipleValues ? null : sourceType.Value, <--- not passing source type anymore, because the user might want to change the action type before changing the source type
                        false);
                    if (!newActionIndex.HasValue)
                        return;

                    actionType.Value = newActionIndex.Value;
                }, actionDataObservable.Select(a => a?.NameReadable ?? "---"), null, actionType);
                
                var canPickTarget = actionDataObservable.Select(actionData => !actionData.HasValue || actionData.Value.TargetTypes != SmartSourceTargetType.None);

                var selectTarget = new EditableActionData("Type", "Target", async () =>
                {
                    var newTargetIndex = await ShowTargetPicker(actionsToEdit[0].Parent,
                        actionType.HoldsMultipleValues
                            ? null
                            : smartDataManager.GetRawData(SmartType.SmartAction, actionType.Value));
                    if (!newTargetIndex.HasValue)
                        return;

                    var newId = newTargetIndex.Value.Item2
                        ? SmartConstants.SourceStoredObject
                        : newTargetIndex.Value.Item1;

                    targetType.Value = newId;
                    if (newTargetIndex.Value.Item2)
                        firstTargetParameter.parameter.Value = newTargetIndex.Value.Item1;
                }, targetDataObservable.Select(t => t?.NameReadable ?? "---"), canPickTarget.Not(), targetType);

                selectSourceTypeCommand = selectSource.Command;
                selectActionTypeCommand = selectAction.Command;
                selectTargetTypeCommand = selectTarget.Command;
                
                if (preferences.ActionEditViewOrder == ActionEditViewOrder.SourceActionTarget)
                {
                    editableGroup.Add(selectSource);
                    editableGroup.Add(selectAction);
                }
                else
                {
                    editableGroup.Add(selectAction);
                    editableGroup.Add(selectSource);
                }
                editableGroup.Add(selectTarget);
                    
                if (editorFeatures.SupportsTargetCondition)
                {
                    var anyHasSourceConditions = actionsToEdit.Any(a => a.Source.Conditions != null && a.Source.Conditions.Count > 0);
                    var anyHasTargetConditions = actionsToEdit.Any(a => a.Target.Conditions != null && a.Target.Conditions.Count > 0);
                    var editingMultipleSourceConditions = anyHasSourceConditions && actionsToEdit.Count > 1;
                    var editingMultipleTargetConditions = anyHasTargetConditions && actionsToEdit.Count > 1;

                    var sourceConditions = new ReactiveProperty<string>(editingMultipleSourceConditions ? "Conditions (warning: multiple actions)" : $"Conditions ({actionsToEdit[0].Source.Conditions?.Count ?? 0})");
                    editableGroup.Add(new EditableActionData("Conditions", "Source", async () =>
                    {
                        var newConditions = await conditionEditService.EditConditions(30, actionsToEdit[0].Source.Conditions);
                        if (newConditions != null)
                        {
                            var conditions = newConditions.ToList();
                            modifiedSourceConditions = true;
                            foreach (var action in actionsToEdit)
                                action.Source.Conditions = conditions.ToList();
                            sourceConditions.Value = $"Conditions ({actionsToEdit[0].Source.Conditions?.Count ?? 0})";
                        }
                    }, sourceConditions));
                    
                    var targetConditions = new ReactiveProperty<string>(editingMultipleTargetConditions ? "Conditions (warning: multiple actions)" : $"Conditions ({actionsToEdit[0].Target.Conditions?.Count ?? 0})");
                    editableGroup.Add(new EditableActionData("Conditions", "Target", async () =>
                    {
                        var newConditions = await conditionEditService.EditConditions(30, actionsToEdit[0].Target.Conditions);
                        if (newConditions != null)
                        {
                            var conditions = newConditions.ToList();
                            modifiedTargetConditions = true;
                            foreach (var action in actionsToEdit)
                                action.Target.Conditions = conditions.ToList();
                            targetConditions.Value = $"Conditions ({actionsToEdit[0].Target.Conditions?.Count ?? 0})";
                        }
                    }, targetConditions, canPickTarget.Not()));
                }
            }

            ParametersEditViewModel viewModel = new(itemFromListProvider,
                currentCoreVersion,
                parameterPickerService,
                actionsToEdit.Count == 1 ? actionsToEdit[0] : null,
                !editOriginal,
                editableGroup,
                () =>
                {
                    using (bulkEdit.BulkEdit(originalActions[0].Id == SmartConstants.ActionComment
                               ? "Edit comment"
                               : "Edit action"))
                    {
                        // if (actionData.ImplicitSource != null)
                        //     smartFactory.UpdateSource(actions.Source, smartDataManager.GetDataByName(SmartType.SmartSource, actionData.ImplicitSource).Id);
                        
                        actionType.ApplyToOriginals();
                        sourceType.ApplyToOriginals();
                        targetType.ApplyToOriginals();
                        editableGroup.Apply();
                        
                        if (!editOriginal && editorFeatures.SupportsTargetCondition)
                        {
                            if (modifiedSourceConditions)
                            {
                                foreach (var action in originalActions)
                                    action.Source.Conditions = actionsToEdit[0].Source.Conditions?.ToList();
                            }
                            if (modifiedTargetConditions)
                            {
                                foreach (var action in originalActions)
                                    action.Target.Conditions = actionsToEdit[0].Target.Conditions?.ToList();
                            }
                        }
                    }
                }, focusFirstGroup: "Action", context: actionsToEdit[0]); 
            viewModel.AutoDispose(actionType);
            viewModel.AutoDispose(sourceType);
            viewModel.AutoDispose(targetType);
            if (selectActionTypeCommand != null)
            {
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectActionTypeCommand()), "Ctrl+A", true));
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectActionTypeCommand()), "Cmd+A", true));
            }
            if (selectSourceTypeCommand != null)
            {
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectSourceTypeCommand()), "Ctrl+S"));
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectSourceTypeCommand()), "Cmd+S"));
            }
            if (selectTargetTypeCommand != null)
            {
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectTargetTypeCommand()), "Ctrl+T"));
                viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectTargetTypeCommand()), "Cmd+T"));
            }
            
            return viewModel;
        }

        private async Task<bool> EditActionCommand(IReadOnlyList<SmartAction> smartActions)
        {
            var viewModel = ActionEditViewModel(smartActions);
            var result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        private Task<bool> EditSelectedActionsCommand()
        {
            var actions = Events.SelectMany(e => e.Actions).Where(a => a.IsSelected).ToList();
            if (actions.Count == 0)
                return Task.FromResult(false);
            return EditActionCommand(actions);
        }
        
        private ParametersEditViewModel ConditionEditViewModel(IReadOnlyList<SmartCondition> originalConditions, bool editOriginal = false)
        {
            Debug.Assert(originalConditions.Count > 0);
            IReadOnlyList<SmartCondition> conditionsToEdit = editOriginal? originalConditions : originalConditions.Select(c => c.Copy()).ToList();
            if (!editOriginal)
                conditionsToEdit.Each(c => c.Parent = originalConditions[0].Parent); // fake parent

            var bulk = new ScriptBulkEdit(script);
            SmartEditableGroup editableGroup = new(bulk);

            editableGroup.Add("General", conditionsToEdit, originalConditions, c => c.Inverted);
            editableGroup.Add("General", conditionsToEdit, originalConditions, c => c.ConditionTarget);

            editableGroup.Add("Condition", conditionsToEdit, originalConditions);
            
            MultiPropertyValueHolder<int, SmartCondition> conditionType = new MultiPropertyValueHolder<int, SmartCondition>(0,
                conditionsToEdit,
                originalConditions,
                e => e.Id,
                (e, id) => smartFactory.UpdateCondition(e, id), bulk);
            
            editableGroup.Add(new EditableActionData("Condition", "General", async () =>
            {
                int? newConditionId = await ShowConditionPicker();
                if (!newConditionId.HasValue)
                    return;

                conditionType.Value = newConditionId.Value;
            }, conditionType.ToObservable(e => e.Value).Select(id => conditionType.HoldsMultipleValues ? "---" : conditionDataManager.GetConditionData(id).NameReadable),
                null, conditionType));
            
            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                currentCoreVersion,
                parameterPickerService,
                conditionsToEdit.Count == 1 ? conditionsToEdit[0] : null,
                !editOriginal,
                editableGroup,
                () =>
                {
                    using (bulk.BulkEdit("Edit conditions"))
                    {
                        conditionType.ApplyToOriginals();
                        editableGroup.Apply();
                    }
                },
                "Condition", conditionsToEdit[0]);
            viewModel.AutoDispose(conditionType);
            
            return viewModel;
        }

        private async Task<bool> EditConditionCommand(IReadOnlyList<SmartCondition> originalCondition)
        {
            var viewModel = ConditionEditViewModel(originalCondition);
            bool result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }
        
        private Task<bool> EditSelectedConditionsCommand()
        {
            var conditions = Events.SelectMany(e => e.Conditions).Where(a => a.IsSelected).ToList();
            if (conditions.Count == 0)
                return Task.FromResult(false);
            return EditConditionCommand(conditions);
        }

        private void EditSelectedEventsCommand()
        {
            if (SelectedItem != null)
                EditEventCommand(Events.Where(ev => ev.IsSelected).ToList()).ListenErrors();
        }

        private class ScriptBulkEdit : IBulkEditSource
        {
            private readonly SmartScript script;

            public ScriptBulkEdit(SmartScript script)
            {
                this.script = script;
            }

            public IDisposable BulkEdit(string name)
            {
                return script.BulkEdit(name);
            }
        }
        
        private ParametersEditViewModel EventEditViewModel(IReadOnlyList<SmartEvent> originalEvents, bool editOriginal = false)
        {
            IReadOnlyList<SmartEvent> eventsToEdit = editOriginal ? originalEvents : originalEvents.Select(e => e.CopyWithConditions()).ToList();
            if (!editOriginal)
                eventsToEdit.Each(e => e.Parent = originalEvents[0].Parent);

            var bulkEdit = new ScriptBulkEdit(script);
            
            SmartEditableGroup editableGroup = new SmartEditableGroup(bulkEdit);
            MultiPropertyValueHolder<int, SmartEvent> eventType = new MultiPropertyValueHolder<int, SmartEvent>(0,
                eventsToEdit,
                originalEvents,
                e => e.Id,
                (e, id) =>
                {
                    smartFactory.UpdateEvent(e, id);
                },
                bulkEdit);
            
            editableGroup.Add("General", eventsToEdit, originalEvents, e => e.Chance);
            editableGroup.Add("General", eventsToEdit, originalEvents, e => e.Flags);
            editableGroup.Add("General", eventsToEdit, originalEvents, e => e.Phases);
            
            if (editorFeatures.SupportsEventCooldown)
            {
                editableGroup.Add("General", eventsToEdit, originalEvents, e => e.CooldownMin);
                editableGroup.Add("General", eventsToEdit, originalEvents, e => e.CooldownMax);
            }
            
            if (editorFeatures.SupportsEventTimerId)
            {
                editableGroup.Add("General", eventsToEdit, originalEvents, e => e.TimerId);
            }

            bool modifiedConditions = false;
            var anyHasConditions = eventsToEdit.Any(e => e.Conditions.Count > 0);
            var editingMultipleConditions = anyHasConditions && eventsToEdit.Count > 1;

            var sourceConditions = new ReactiveProperty<string>(editingMultipleConditions ? "Conditions (warning: multiple events)" : $"Conditions ({eventsToEdit[0].Conditions?.Count ?? 0})");
            editableGroup.Add(new EditableActionData("Conditions", "General", async () =>
            {
                var oldConditions = smartScriptExporter.ToDatabaseCompatibleConditions(script, eventsToEdit[0]);
                var newConditions = await conditionEditService.EditConditions(22, oldConditions);
                if (newConditions != null)
                {
                    var conditions = newConditions.ToList();
                    var smartConditions = smartScriptImporter.ImportConditions(script, conditions);
                    modifiedConditions = true;
                    foreach (var @event in eventsToEdit)
                    {
                        @event.Conditions.RemoveAll();
                        foreach (var c in smartConditions)
                            @event.Conditions.Add(c.Copy());
                    }
                    sourceConditions.Value = $"Conditions ({eventsToEdit[0].Conditions?.Count ?? 0})";
                }
            }, sourceConditions));

            var parameters = editableGroup.Add("Event specific", eventsToEdit, originalEvents);
            
            var selectEventTypeCommand = editableGroup.Add(new EditableActionData("Event", "General", async () =>
            {
                int? newEventIndex = await ShowEventPicker();
                if (!newEventIndex.HasValue)
                    return;

                using var _ = bulkEdit.BulkEdit("Change event type");
                eventType.Value = newEventIndex.Value;
                FillNonzeroWithDefaults(SmartType.SmartEvent, newEventIndex.Value, parameters);
            }, eventType.ToObservable(e => e.Value).Select(id => eventType.HoldsMultipleValues ? "--" : smartDataManager.GetRawData(SmartType.SmartEvent, id).NameReadable),
                null, eventType)).Command;
            
            ParametersEditViewModel viewModel = new(itemFromListProvider, 
                currentCoreVersion,
                parameterPickerService,
                eventsToEdit.Count == 1 ? eventsToEdit[0] : null,
                !editOriginal,
                editableGroup,
                () =>
                {
                    using (bulkEdit.BulkEdit("Edit events"))
                    {
                        eventType.ApplyToOriginals();
                        editableGroup.Apply();
                        if (!editOriginal && modifiedConditions)
                        {
                            foreach (var e in originalEvents)
                            {
                                e.Conditions.RemoveAll();
                                foreach (var c in eventsToEdit[0].Conditions)
                                    e.Conditions.Add(c.Copy());
                            }
                        }
                    }
                },
                "Event specific", context: eventsToEdit[0]);
            viewModel.AutoDispose(eventType);

            viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectEventTypeCommand()), "Ctrl+E"));
            viewModel.KeyBindings.Add(new CommandKeyBinding(new AsyncAutoCommand(() => selectEventTypeCommand()), "Cmd+E"));

            return viewModel;
        }

        private void FillNonzeroWithDefaults(SmartType dataType, int newIndex, List<MultiParameterValueHolder<long>> parameters)
        {
            var newEventData = smartDataManager.GetRawData(dataType, newIndex);
            if (newEventData.Parameters != null)
            {
                for (int i = 0; i < Math.Min(newEventData.Parameters.Count, parameters.Count); ++i)
                {
                    var parameterData = newEventData.Parameters[i];
                    if (parameterData.DefaultVal != 0 && !parameters[i].HoldsMultipleValues && parameters[i].Value == 0)
                        parameters[i].Value = parameterData.DefaultVal;
                }
            }
        }

        private async Task<bool> EditEventCommand(IReadOnlyList<SmartEvent> originalEvents)
        {
            var viewModel = EventEditViewModel(originalEvents);
            bool result = await windowManager.ShowDialog(viewModel);
            viewModel.Dispose();
            return result;
        }

        public ISolutionItem SolutionItem => item;

        private ISmartScriptOutlinerModel outlinerData;
        private readonly ISmartHighlighter highlighter;
        private ValuePublisher<IOutlinerModel> outlinerModel = new ValuePublisher<IOutlinerModel>();
        public IObservable<IOutlinerModel> OutlinerModel => outlinerModel;

        public IObservable<IReadOnlyList<IInspectionResult>> Problems => problems;
        private ReactiveProperty<IReadOnlyList<IInspectionResult>> problems = new(new List<IInspectionResult>());

        private Dictionary<int, DiagnosticSeverity>? problematicLines = new();

        public Dictionary<int, DiagnosticSeverity>? ProblematicLines
        {
            get => problematicLines;
            set => SetProperty(ref problematicLines, value);
        }

        public ObservableCollection<SmartExtensionNotification> Notifications { get; } = new();

        public DelegateCommand<SmartExtensionNotification> DismissNotification { get; }

        public void AddNotification(SmartExtensionNotification notification)
        {
            Notifications.Add(notification.WrapCommand(cmd => new DelegateCommand(() =>
            {
                cmd.Execute(null);
                DismissNotification.Execute(notification);
            })));
            notification.AutoDispose(notification
                .ToObservable(o => o.IsOpened)
                .SubscribeAction(isOpened =>
                {
                    if (isOpened)
                        return;

                    DismissNotification.Execute(notification);
                }));
        }
        
        private void RecolorEvent(SmartEvent e)
        {
            highlighter.SetHighlightParameter(selectedHighlighter.Parameter);
            e.ColorId = (0, 0);
            if (highlighter.Highlight(e, out var eventParameterIndex))
            {
                var value = e.GetParameter(eventParameterIndex).Value;
                e.ColorId = (selectedHighlighter.HighlightIndex, value);
            }
            foreach (var a in e.Actions)
            {
                a.ColorId = (0, 0);
                if (highlighter.Highlight(a, out var actionParameterIndex))
                {
                    var value = a.GetParameter(actionParameterIndex).Value;
                    a.ColorId = (selectedHighlighter.HighlightIndex, value);
                }
            }
            foreach (var c in e.Conditions)
            {
                c.ColorId = (0, 0);
                if (highlighter.Highlight(c, out var conditionParameterIndex))
                {
                    var value = c.GetParameter(conditionParameterIndex).Value;
                    c.ColorId = (selectedHighlighter.HighlightIndex, value);
                }
            }
        }
    }
}
