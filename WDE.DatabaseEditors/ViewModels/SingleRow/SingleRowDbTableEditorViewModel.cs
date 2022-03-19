using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using AvaloniaStyles.Controls.FastTableView;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using ReactiveUI;
using WDE.Common;
using WDE.Common.Avalonia;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Sessions;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.CustomCommands;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.History.SingleRow;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.MVVM;
using WDE.Parameters.Parameters;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels.SingleRow
{
    public partial class SingleRowDbTableEditorViewModel : ViewModelBase
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly IMySqlExecutor mySqlExecutor;
        private readonly IQueryGenerator queryGenerator;
        private readonly IDatabaseTableModelGenerator modelGenerator;
        private readonly IConditionEditService conditionEditService;
        private readonly IDatabaseEditorsSettings editorSettings;
        private readonly ITableEditorPickerService tableEditorPickerService;
        private readonly IMainThread mainThread;
        private readonly IPersonalGuidRangeService personalGuidRangeService;
        private readonly IDatabaseTableDataProvider tableDataProvider;

        private HashSet<DatabaseKey> keys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> removedKeys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> forceInsertKeys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> forceDeleteKeys = new HashSet<DatabaseKey>();
        private HashSet<(DatabaseKey key, string columnName)> forceUpdateCells = new HashSet<(DatabaseKey, string)>();
        private List<System.IDisposable> rowsDisposable = new();
        public ObservableCollection<DatabaseEntityViewModel> Rows { get; } = new();

        private bool showOnlyModified;
        [AlsoNotify(nameof(SelectedEntity))] [AlsoNotify(nameof(SelectedRow))] [Notify] private int selectedRowIndex;
        [AlsoNotify(nameof(SelectedCell))] [Notify] private int selectedCellIndex;
        public override DatabaseEntity? SelectedEntity => SelectedRow?.Entity;
        public DatabaseEntityViewModel? SelectedRow => selectedRowIndex >= 0 && selectedRowIndex < Rows.Count ? Rows[selectedRowIndex] : null;
        public SingleRecordDatabaseCellViewModel? SelectedCell => SelectedRow != null && selectedCellIndex >= 0 && selectedCellIndex < SelectedRow.Cells.Count ? SelectedRow.Cells[selectedCellIndex] : null;
        [Notify] private string rowsSummaryText = "";
        [Notify] private int limitQuery = 300;
        [Notify] private long offsetQuery = 0;
        public override ICommand Copy { get; }
        public override ICommand Paste { get; }

        private bool ignoreShowOnlyModifiedEvents;
        public bool ShowOnlyModified
        {
            get => showOnlyModified;
            set
            {
                if (showOnlyModified == value)
                    return;
                if (ignoreShowOnlyModifiedEvents)
                    return;
                ignoreShowOnlyModifiedEvents = true;
                
                async Task ScheduleLoadingAndUpdateOnlyModified(bool showOnlyModified)
                {
                    if (!await BeforeLoadData())
                    {
                        ignoreShowOnlyModifiedEvents = false;
                        return;                        
                    }
                    History.MarkAsSaved(); // workaround for double showing message regarding unsaved changes
                    this.showOnlyModified = showOnlyModified;
                    OffsetQuery = 0;
                    await ScheduleLoading();
                    RaisePropertyChanged(nameof(ShowOnlyModified));
                    ignoreShowOnlyModifiedEvents = false;
                }

                ScheduleLoadingAndUpdateOnlyModified(value).ListenErrors();
            }
        }
        
        private MultiRowSplitMode splitMode;
        public bool SplitView => splitMode != MultiRowSplitMode.None;

        public bool SplitHorizontal
        {
            get => splitMode == MultiRowSplitMode.Horizontal;
            set
            {
                if (value && splitMode == MultiRowSplitMode.Horizontal)
                    return;
                
                splitMode = value ? MultiRowSplitMode.Horizontal : (splitMode == MultiRowSplitMode.Horizontal ? MultiRowSplitMode.None : splitMode);
                editorSettings.MultiRowSplitMode = splitMode;
                RaisePropertyChanged(nameof(SplitHorizontal));
                RaisePropertyChanged(nameof(SplitVertical));
                RaisePropertyChanged(nameof(SplitView));
            }
        }
        public bool SplitVertical
        {
            get => splitMode == MultiRowSplitMode.Vertical;
            set
            {
                if (value && splitMode == MultiRowSplitMode.Vertical)
                    return;
                
                splitMode = value ? MultiRowSplitMode.Vertical : (splitMode == MultiRowSplitMode.Vertical ? MultiRowSplitMode.None : splitMode);
                editorSettings.MultiRowSplitMode = splitMode;
                RaisePropertyChanged(nameof(SplitHorizontal));
                RaisePropertyChanged(nameof(SplitVertical));
                RaisePropertyChanged(nameof(SplitView));
            }
        }

        private IList<DatabaseColumnJson> columns = new List<DatabaseColumnJson>();
        public ObservableCollection<DatabaseColumnHeaderViewModel> Columns { get; } = new();
        public ObservableCollection<int> HiddenColumns { get; } = new();
        public MySqlFilterViewModel FilterViewModel { get; }

        public AsyncAutoCommand AddNewCommand { get; }
        public DelegateCommand<SingleRecordDatabaseCellViewModel?> RevertCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel?> EditConditionsCommand { get; }
        public DelegateCommand<SingleRecordDatabaseCellViewModel?> SetNullCommand { get; }
        public DelegateCommand<SingleRecordDatabaseCellViewModel?> DuplicateCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel> OpenParameterWindow { get; }
        
        public AsyncAutoCommand DeleteRowSelectedCommand { get; }
        public DelegateCommand SetNullSelectedCommand { get; }
        public AsyncAutoCommand DuplicateSelectedCommand { get; }
        public AsyncAutoCommand RevertSelectedCommand { get; }
        
        public AsyncAutoCommand PreviousDataPage { get; }
        public AsyncAutoCommand RefreshQuery { get; }
        public AsyncAutoCommand NextDataPage { get; }
        
        public SingleRowDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider,
            IHistoryManager history, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator, ISolutionManager solutionManager, 
            IParameterFactory parameterFactory, ISolutionTasksService solutionTasksService,
            ISolutionItemNameRegistry solutionItemName, IMySqlExecutor mySqlExecutor,
            IQueryGenerator queryGenerator, IDatabaseTableModelGenerator modelGenerator,
            ITableDefinitionProvider tableDefinitionProvider,
            IConditionEditService conditionEditService, ISolutionItemIconRegistry iconRegistry,
            ISessionService sessionService, IDatabaseEditorsSettings editorSettings,
            IDatabaseTableCommandService commandService,
            IParameterPickerService parameterPickerService,
            IStatusBar statusBar, ITableEditorPickerService tableEditorPickerService,
            IMainThread mainThread, IPersonalGuidRangeService personalGuidRangeService,
            IClipboardService clipboardService)
            : base(history, solutionItem, solutionItemName, 
            solutionManager, solutionTasksService, eventAggregator, 
            queryGenerator, tableDataProvider, messageBoxService, taskRunner, parameterFactory,
            tableDefinitionProvider, itemFromListProvider, iconRegistry, sessionService, commandService,
            parameterPickerService, statusBar, mySqlExecutor)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.solutionItem = solutionItem;
            this.tableDataProvider = tableDataProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.mySqlExecutor = mySqlExecutor;
            this.queryGenerator = queryGenerator;
            this.modelGenerator = modelGenerator;
            this.conditionEditService = conditionEditService;
            this.editorSettings = editorSettings;
            this.tableEditorPickerService = tableEditorPickerService;
            this.mainThread = mainThread;
            this.personalGuidRangeService = personalGuidRangeService;

            splitMode = editorSettings.MultiRowSplitMode;

            OpenParameterWindow = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel>(EditParameter);
            RevertCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(cell =>
            {
                cell!.ParameterValue!.Revert();
            }, cell => cell != null && cell.CanBeReverted && (cell?.TableField?.IsModified ?? false));
            SetNullCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(SetToNull, vm => vm != null && vm.CanBeSetToNull);
            DuplicateCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(Duplicate, vm => vm != null);
            EditConditionsCommand = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel?>(EditConditions);
            AddNewCommand = new AsyncAutoCommand(AddNewEntity);

            SetNullSelectedCommand = new DelegateCommand(() =>
            {
                SelectedCell?.ParameterValue?.SetNull();
            }, () => SelectedCell != null && SelectedCell.CanBeSetToNull).ObservesProperty(() => SelectedCell);

            RevertSelectedCommand = new AsyncAutoCommand(async () =>
            {
                SelectedCell?.ParameterValue?.Revert();
            }, () => SelectedCell != null && SelectedCell.CanBeReverted);
            AutoDispose(this.ToObservable(() => SelectedCell).Subscribe(_ => RevertSelectedCommand.RaiseCanExecuteChanged()));

            DeleteRowSelectedCommand = new AsyncAutoCommand(async () =>
            {
                ForceRemoveEntity(SelectedRow!.Entity);
            }, () => SelectedRow != null);
            AutoDispose(this.ToObservable(() => SelectedRow).Subscribe(_ => DeleteRowSelectedCommand.RaiseCanExecuteChanged()));
            
            DuplicateSelectedCommand = new AsyncAutoCommand(async () =>
            {
                var duplicate = SelectedRow!.Entity.Clone(DatabaseKey.PhantomKey, false);
                await SetupPersonalGuidValue(duplicate);
                ForceInsertEntity(duplicate, SelectedRowIndex + 1);
            }, () => SelectedRow != null);
            AutoDispose(this.ToObservable(() => SelectedRow).Subscribe(_ => DuplicateSelectedCommand.RaiseCanExecuteChanged()));
                
            foreach (var key in solutionItem.Entries.Select(x => x.Key))
                keys.Add(key);
            
            AutoDispose(new ActionDisposable(() =>
            {
                rowsDisposable.ForEach(x => x.Dispose());
                rowsDisposable.Clear();
            }));

            PreviousDataPage = new AsyncAutoCommand(() =>
            {
                OffsetQuery = Math.Max(0, OffsetQuery - LimitQuery);
                return ScheduleLoading();
            });
            NextDataPage = new AsyncAutoCommand(() =>
            {
                OffsetQuery = Math.Max(0, OffsetQuery + LimitQuery);
                return ScheduleLoading();
            });
            RefreshQuery = new AsyncAutoCommand(ScheduleLoading);

            Copy = new DelegateCommand(() =>
            {
                clipboardService.SetText(SelectedCell!.StringValue ?? "(null)");
            }, () => SelectedCell != null).ObservesProperty(()=>SelectedCell);

            Paste = new AsyncAutoCommand(async () =>
            {
                if (await clipboardService.GetText() is { } text)
                {
                    SelectedCell?.UpdateFromString(text);
                }
            }, () => SelectedCell != null);
            
            var definition = tableDefinitionProvider.GetDefinition(solutionItem.DefinitionId);
            FilterViewModel = new MySqlFilterViewModel(definition, ScheduleLoading, parameterFactory, parameterPickerService);
            
            var pseudoItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
            var savedItem = sessionService.Find(pseudoItem);
            if (savedItem is DatabaseTableSolutionItem savedTableItem)
            {
                foreach (var removed in savedTableItem.DeletedEntries)
                    removedKeys.Add(removed);

                foreach (var entries in savedTableItem.Entries)
                {
                    if (solutionItem.Entries.All(e => e.Key != entries.Key))
                        solutionItem.Entries.Add(entries);
                    keys.Add(entries.Key);
                }
            }
            
            ScheduleLoading();
        }

        public override DatabaseEntity AddRow(DatabaseKey key)
        {
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key, true);
            ForceInsertEntity(freshEntity, Entities.Count);
            return freshEntity;
        }
        
        private async Task<bool> CheckIfKeyExistsAndWarn(DatabaseKey key)
        {
            if (await ContainsKey(key))
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Key already added")
                    .SetMainInstruction($"Key {key} is already added to this editor")
                    .SetContent("You cannot add it again, just edit existing.")
                    .WithOkButton(true)
                    .SetIcon(MessageBoxIcon.Error)
                    .Build());
                return false;
            }

            return true;
        }

        private async Task SetupPersonalGuidValue(DatabaseEntity entity)
        {
            if (TableDefinition.AutoKeyValue.HasValue && personalGuidRangeService.IsConfigured)
            {
                var nextGuid = await personalGuidRangeService.GetNextGuidOrShowError(TableDefinition.AutoKeyValue.Value, statusBar);
                if (nextGuid.HasValue)
                    entity.SetTypedCellOrThrow(TableDefinition.PrimaryKey[0], (long)nextGuid.Value);
            }
        }

        private async Task AddNewEntity()
        {
            DatabaseKey key = new DatabaseKey();
            var index = SelectedRow == null ? Entities.Count : selectedRowIndex + 1;
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key, true);
            await SetupPersonalGuidValue(freshEntity);
            ForceInsertEntity(freshEntity, index);
        }
        
        private void SetToNull(SingleRecordDatabaseCellViewModel? view)
        {
            if (view != null && view.CanBeNull && !view.IsReadOnly) 
                view.ParameterValue?.SetNull();
        }

        private void Duplicate(SingleRecordDatabaseCellViewModel? view)
        {
            if (view != null)
            {
                var duplicate = view.Parent.Entity.Clone();
                ForceInsertEntity(duplicate, 0);
            }
        }

        private async Task EditConditions(SingleRecordDatabaseCellViewModel? view)
        {
            if (view == null)
                return;
            
            var conditionList = view.ParentEntity.Conditions;
            
            var newConditions = await conditionEditService.EditConditions(tableDefinition.Condition!.SourceType, conditionList);
            if (newConditions == null)
                return;

            view.ParentEntity.Conditions = newConditions.ToList();
            if (tableDefinition.Condition.SetColumn != null)
            {
                var hasColumn = view.ParentEntity.GetCell(tableDefinition.Condition.SetColumn);
                if (hasColumn is DatabaseField<long> lf)
                    lf.Current.Value = view.ParentEntity.Conditions.Count > 0 ? 1 : 0;
            }
        }

        private Task EditParameter(SingleRecordDatabaseCellViewModel cell)
        {
            if (cell.ParameterValue != null)
                return EditParameter(cell.ParameterValue);
            return Task.CompletedTask;
        }

        // TODO
        protected override IReadOnlyList<DatabaseKey> GenerateKeys() => keys.ToList();
        protected override IReadOnlyList<DatabaseKey>? GenerateDeletedKeys() => GenerateDeletedKeys(false);
        protected IReadOnlyList<DatabaseKey>? GenerateDeletedKeys(bool saveQuery)
        {
            return removedKeys.Count == 0 ? forceDeleteKeys.ToList() : removedKeys.Union(forceDeleteKeys).ToList();
        }

        protected override async Task<DatabaseTableData?> LoadData()
        {
            return await databaseTableDataProvider.Load(solutionItem.DefinitionId, FilterViewModel.BuildWhere(), offsetQuery, limitQuery, showOnlyModified ? keys.ToArray() : null) as DatabaseTableData; ;
        }

        private DatabaseKey? beforeLoadSelectedRow;
        protected override async Task<bool> BeforeLoadData()
        {
            if (IsModified)
            {
                var result = await messageBoxService.ShowDialog(new MessageBoxFactory<int>()
                    .SetTitle("Save changes")
                    .SetMainInstruction("You have unsaved changes. Do you want to save them?")
                    .SetContent("Changing the rows you see will discard unsaved changes.")
                    .WithYesButton(0)
                    .WithNoButton(1)
                    .WithCancelButton(2)
                    .Build());
                if (result == 2)
                    return false;
                if (result == 0)
                    await SaveDataNow();
            }
            beforeLoadSelectedRow = SelectedRow?.Entity?.GenerateKey(TableDefinition);
            // save existing changes
            SelectedRowIndex = -1;
            UpdateSolutionItem();

            if (historyHandler != null)
            {
                historyHandler.Dispose();
                History.RemoveHandler(historyHandler);
                historyHandler = null;
            }
            return true;
        }

        private async Task SaveDataNow()
        {
            UpdateSolutionItem();
            solutionManager.Refresh(SolutionItem);
            await mySqlExecutor.ExecuteSql(await GenerateQuery());
            await sessionService.UpdateQuery(this);
            MaterializePhantomEntities();
            History.MarkAsSaved();
        }
        
        protected override async Task InternalLoadData(DatabaseTableData data)
        {
            History.Clear();
            rowsDisposable.ForEach(x => x.Dispose());
            rowsDisposable.Clear();
            Rows.RemoveAll();
            if (columns.Count == 0)
            {
                columns = tableDefinition.Groups.SelectMany(g => g.Fields).ToList();
                Debug.Assert(Columns.Count == 0);
                Columns.AddRange(columns.Select(c => new DatabaseColumnHeaderViewModel(c)));
                Columns.Each((col, i) => AutoDispose(col.ToObservable(x => x.IsVisible)
                    .Subscribe(@is =>
                    {
                        if (@is)
                        {
                            if (HiddenColumns.Contains(i))
                                HiddenColumns.Remove(i);
                        }
                        else
                        {
                            if (!HiddenColumns.Contains(i))
                                HiddenColumns.Add(i);
                        }
                    })));
            }

            await AsyncAddEntities(data.Entities);

            SelectedRowIndex = beforeLoadSelectedRow.HasValue ? Entities.IndexIf(e => e.GenerateKey(TableDefinition) == beforeLoadSelectedRow) : (Entities.Count > 0 ? 0 : -1);
            
            var allRows = await tableDataProvider.GetCount(tableDefinition.Id, FilterViewModel.BuildWhere(), showOnlyModified ? keys : null);
            var start = allRows == 0 ? 0 : offsetQuery + 1;
            RowsSummaryText = $"{start} - {offsetQuery + data.Entities.Count} of {allRows} rows";
            historyHandler = History.AddHandler(new SingleRowTableEditorHistoryHandler(this));
        }

        public string SerializeDefinition(DatabaseTableDefinitionJson json)
        {
            var settings = CreateJsonSerializationSettings();
            return JsonConvert.SerializeObject(json, Formatting.Indented, settings);
        }

        private JsonSerializerSettings CreateJsonSerializationSettings()
        {
            var settings = new JsonSerializerSettings();
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            return settings;
        }
        
        protected override IDisposable BulkEdit(string name) => historyHandler?.BulkEdit(name) ?? Disposable.Empty;

        private SingleRowTableEditorHistoryHandler? historyHandler;

        protected override void UpdateSolutionItem()
        {
            var previousData = solutionItem.Entries.Where(e => Entities.All(entity => entity.GenerateKey(TableDefinition) != e.Key));
            
            solutionItem.Entries = Entities.Where(e => (!e.ExistInDatabase || EntityIsModified(e)) && !e.Phantom)
                .Select(e => new SolutionItemDatabaseEntity(e.Key, e.ExistInDatabase, GetOriginalFields(e)))
                .Union(previousData)
                .ToList();
            solutionItem.DeletedEntries = removedKeys.ToList();
        }

        private bool EntityIsModified(DatabaseEntity databaseEntity)
        {
            // todo: optimize?
            return databaseEntity.Cells.Any(c => c.Value.IsModified);
        }

        public void RedoExecuteDelete(DatabaseEntity entity)
        {
            if (mySqlExecutor.IsConnected)
            {
                mySqlExecutor.ExecuteSql(queryGenerator.GenerateDeleteQuery(tableDefinition, entity));
                History.MarkNoSave();
            }
        }
        
        public override bool ForceRemoveEntity(DatabaseEntity entity)
        {
            var indexOfEntity = Entities.IndexOf(entity);
            Debug.Assert(indexOfEntity >= 0);

            if (!entity.Phantom)
            {
                keys.Remove(entity.Key);
                removedKeys.Add(entity.Key);
                forceInsertKeys.Remove(entity.Key);
            }
            
            Entities.RemoveAt(indexOfEntity);
            if (SelectedRow?.Entity == entity)
                SelectedRowIndex = -1;
            
            Rows[indexOfEntity].ChangedCell -= OnRowChangedCell;
            Rows[indexOfEntity].Changed -= OnRowChanged;
            Rows.RemoveAt(indexOfEntity);

            return true;
        }
        
        public async Task<bool> AddEntity(DatabaseEntity entity)
        {
            return ForceInsertEntity(entity, Entities.Count);
        }

        public override bool ForceInsertEntity(DatabaseEntity entity, int index, bool undoing = false)
        {
            if (!entity.Phantom)
            {
                if (undoing)
                    forceInsertKeys.Add(entity.Key);
                removedKeys.Remove(entity.Key);
                forceDeleteKeys.Remove(entity.Key);
                var pseudoItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
                var savedItem = sessionService.Find(pseudoItem);
                if (savedItem is DatabaseTableSolutionItem savedTableItem)
                    savedTableItem.UpdateEntitiesWithOriginalValues(new List<DatabaseEntity>(){entity});
            }

            var row = new DatabaseEntityViewModel(entity);
            row.Changed += OnRowChanged;
            row.ChangedCell += OnRowChangedCell;
            AutoDisposeEntity(new ActionDisposable(() =>
            {
                row.ChangedCell -= OnRowChangedCell;
                row.Changed -= OnRowChanged;
            }));
            
            int columnIndex = 0;
            foreach (var column in columns)
            {
                SingleRecordDatabaseCellViewModel cellViewModel;

                if (column.IsConditionColumn)
                {
                    var label = entity.ToObservable(e => e.Conditions).Select(c => "Edit (" + (c?.Count ?? 0) + ")");
                    cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, "Conditions", EditConditionsCommand, row, entity, label));
                }
                else if (column.IsMetaColumn)
                {
                    if (column.Meta!.StartsWith("table:"))
                    {
                        var table = column.Meta.Substring(6, column.Meta.IndexOf(";", 6) - 6);
                        var condition = column.Meta.Substring(column.Meta.IndexOf(";", 6) + 1);
                        cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, column.Name, new DelegateCommand(
                            () =>
                            {
                                var newCondition = condition;
                                int indexOf = 0;
                                indexOf = newCondition.IndexOf("{", indexOf);
                                while (indexOf != -1)
                                {
                                    var columnName = newCondition.Substring(indexOf + 1, newCondition.IndexOf("}", indexOf) - indexOf - 1);
                                    newCondition = newCondition.Replace("{" + columnName + "}", entity.GetCell(columnName)!.ToString());
                                    indexOf = newCondition.IndexOf("{", indexOf + 1);
                                }
                                tableEditorPickerService.ShowTable(table, newCondition);
                            }), row, entity, "Open"));
                    }
                    else if (column.Meta!.StartsWith("one2one:"))
                    {
                        var table = column.Meta.Substring(8);
                        cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, column.Name, new DelegateCommand(
                            () =>
                            {
                                tableEditorPickerService.ShowForeignKey1To1(table, entity.Key);
                            }, () => !entity.Phantom), row, entity, "Open"));
                    }
                    else
                        throw new Exception("Unsupported meta column: " + column.Meta);
                }
                else
                {
                    var cell = entity.GetCell(column.DbColumnName);
                    if (cell == null)
                        throw new Exception("this should never happen");

                    IParameterValue parameterValue = null!;
                    if (cell is DatabaseField<long> longParam)
                    {
                        IParameter<long> parameter = parameterFactory.Factory(column.ValueType);
                        parameterValue = new ParameterValue<long, DatabaseEntity>(entity, longParam.Current, longParam.Original, parameter);
                    }
                    else if (cell is DatabaseField<string> stringParam)
                    {
                        if (column.AutogenerateComment != null)
                        {
                            stringParam.Current.Value = stringParam.Current.Value.GetComment(column.CanBeNull);
                            stringParam.Original.Value = stringParam.Original.Value.GetComment(column.CanBeNull);
                        }
                        parameterValue = new ParameterValue<string, DatabaseEntity>(entity, stringParam.Current, stringParam.Original, parameterFactory.FactoryString(column.ValueType));
                    }
                    else if (cell is DatabaseField<float> floatParameter)
                    {
                        parameterValue = new ParameterValue<float, DatabaseEntity>(entity, floatParameter.Current, floatParameter.Original, FloatParameter.Instance);
                    }

                    parameterValue.DefaultIsBlank = column.IsZeroBlank;

                    cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, column, row, entity, cell, parameterValue));
                }
                row.Cells.Add(cellViewModel);
                columnIndex++;
            }
            
            Entities.Insert(index, entity);
            Rows.Insert(index, row);
            selectedRowIndex = -1;
            SelectedRowIndex = index;
            return true;
        }

        private void OnRowChangedCell(DatabaseEntityViewModel entity, SingleRecordDatabaseCellViewModel cell, string columnName)
        {
            if (tableDefinition.GroupByKeys.Contains(columnName) && !entity.IsPhantomEntity)
                ReKey(entity).ListenErrors();
            else
            {
                if (!cell.IsModified && entity.Entity.ExistInDatabase)
                {
                    Debug.Assert(!entity.Entity.Phantom);
                    forceUpdateCells.Add((entity.Entity.Key, columnName));
                    History.MarkNoSave();
                }
            }
        }

        private async Task ReKey(DatabaseEntityViewModel entity)
        {   
            Debug.Assert(!entity.IsPhantomEntity);
            DatabaseKey BuildKey(DatabaseEntity e)
            {
                return new DatabaseKey(tableDefinition.GroupByKeys.Select(e.GetTypedValueOrThrow<long>));
            }

            var newRealKey = BuildKey(entity.Entity);
            var newKey = DatabaseKey.PhantomKey;
            var oldKey = entity.Entity.Key;
            var originalExistsInDb = entity.Entity.ExistInDatabase;

            if (newRealKey == entity.Entity.Key)
                return; // no re-keying needed
            
            // if (await ContainsKey(newRealKey))
            // {
            //     int i = 0;
            //     // revert values
            //     foreach (var column in tableDefinition.GroupByKeys)
            //         entity.Entity.SetTypedCellOrThrow(column, entity.Entity.Key[i++]);
            //
            //     await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
            //         .SetTitle("Duplicate key")
            //         .SetMainInstruction($"The key {newRealKey} is already in the database.")
            //         .SetContent("If you want to change existing key, just modify it.\n\nIf you want to revert the key back to the previous value, _save the changes_ and then re-key again.")
            //         .WithOkButton(false)
            //         .Build());
            // }
            //else
            int indexOfRow = Rows.IndexOf(entity);

            {
                historyHandler!.DoAction(new AnonymousHistoryAction("Change key", () =>
                {
                    var old = Rows[indexOfRow];
                    var original = old.Entity.Clone(oldKey, originalExistsInDb);
                    ForceRemoveEntity(old.Entity);
                    ForceInsertEntity(original, indexOfRow);
                    int i = 0;
                    foreach (var column in tableDefinition.GroupByKeys)
                        original.SetTypedCellOrThrow(column, oldKey[i++]);
                    Debug.Assert(!oldKey.IsPhantomKey);
                    forceInsertKeys.Add(oldKey);
                }, () =>
                {
                    var old = Rows[indexOfRow];
                    var clone = old.Entity.Clone(newKey, false);
                    ForceRemoveEntity(old.Entity);
                    ForceInsertEntity(clone, indexOfRow);
                    int i = 0;
                    foreach (var column in tableDefinition.GroupByKeys)
                        clone.SetTypedCellOrThrow(column, newRealKey[i++]);
                }));
            }
        }

        private void OnRowChanged(ITableRow obj)
        {
            if (obj is not DatabaseEntityViewModel row)
                return;

            if (row.IsPhantomEntity)
                return;
            
            if (EntityIsModified(row.Entity))
                keys.Add(row.Entity.Key);
            else
                keys.Remove(row.Entity.Key);
        }

        private async Task<bool> ContainsKey(DatabaseKey key)
        {
            if (Rows.Any(row => !row.IsPhantomEntity && row.Entity.Key == key))
                return true;

            return await databaseTableDataProvider.GetCount(tableDefinition.Id, null, new [] { key }) > 0;
        }
        
        private async Task AsyncAddEntities(IReadOnlyList<DatabaseEntity> tableDataEntities)
        {
            foreach (var entity in tableDataEntities)
            {
                await AddEntity(entity);
            }
            selectedRowIndex = -1;
            if (Rows.Count > 0)
                SelectedRowIndex = 0;
        }

        public override async Task<IList<(ISolutionItem, IQuery)>> GenerateSplitQuery()
        {
            var sql = await GenerateQuery();
            return new List<(ISolutionItem, IQuery)>() { (solutionItem, sql) };
        }
        
        private T AutoDisposeEntity<T>(T entity) where T : IDisposable
        {
            rowsDisposable.Add(entity);
            return entity;
        }

        protected override Task<IQuery> GenerateSaveQuery()
        {
            return GenerateQueryImpl(true);
        }

        public override Task<IQuery> GenerateQuery()
        {
            return GenerateQueryImpl(false);
        }

        private async Task<IQuery> GenerateQueryImpl(bool saveQuery)
        {
            var newData = Entities.Where(e => (!e.ExistInDatabase || EntityIsModified(e) || e.Phantom) || (saveQuery && !e.Phantom && forceInsertKeys.Contains(e.Key))).ToList();

            if (saveQuery)
            {
                foreach (var e in forceInsertKeys)
                {
                    var entity = Entities.Where(x => !x.Phantom).FirstOrDefault(x => x.Key == e);
                    if (entity == null)
                        continue;
                    newData.Add(entity.Clone(null, false));
                }
            }

            var newDataKeys = newData.Select(e => e.GenerateKey(TableDefinition)).ToArray();
            var oldKeys = keys.Except(newDataKeys).ToArray();
            
            IDatabaseTableData? data = null;
            if (oldKeys.Length > 0)
            {
                data = await databaseTableDataProvider.Load(tableDefinition.Id, null, null, keys.Count, oldKeys);
                if (data == null)
                    return Queries.Raw("ERROR");
                solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);
            }

            data = new DatabaseTableData(tableDefinition, data == null ? newData : data.Entities.Union(newData).ToList());
            
            var query = queryGenerator.GenerateQuery(GenerateKeys(), GenerateDeletedKeys(saveQuery), data);

            if (!saveQuery)
                return query;

            IMultiQuery multi = Queries.BeginTransaction();
            multi.Add(query);
            foreach (var pair in forceUpdateCells)
            {
                var entity = Entities.Where(x => !x.Phantom).FirstOrDefault(x => x.Key == pair.key);
                var field = entity?.GetCell(pair.columnName);
                if (entity == null || field == null)
                    continue;
                multi.Add(queryGenerator.GenerateUpdateFieldQuery(tableDefinition, entity, field));
            }
            return multi.Close();
        }

        protected override Task AfterSave()
        {
            forceInsertKeys.Clear();
            forceUpdateCells.Clear();
            forceDeleteKeys.Clear();
            MaterializePhantomEntities();
            UpdateSolutionItem();
            return Task.CompletedTask;
        }

        private void MaterializePhantomEntities()
        {
            List<int> indicesToMaterialize = null!;
            for (int i = Entities.Count - 1; i >= 0; --i)
            {
                if (!Entities[i].Phantom)
                    continue;
                indicesToMaterialize ??= new();
                indicesToMaterialize.Add(i);
            }
            if (indicesToMaterialize != null)
            {
                historyHandler!.DoAction(new AnonymousHistoryAction("Materialized phantom entities", () =>
                {
                    foreach (var i in indicesToMaterialize)
                    {
                        keys.Remove(Entities[i].Key);
                        forceDeleteKeys.Add(Entities[i].Key);
                        var clone = Entities[i].Clone(DatabaseKey.PhantomKey);
                        ForceRemoveEntity(Entities[i]);
                        ForceInsertEntity(clone, i);
                    }
                }, () =>
                {
                    foreach (var i in indicesToMaterialize)
                    {
                        var realKey = Entities[i].GenerateKey(TableDefinition);
                        var clone = Entities[i].Clone(realKey);
                        ForceRemoveEntity(Entities[i]);
                        ForceInsertEntity(clone, i);
                        keys.Add(realKey);
                        forceDeleteKeys.Remove(Entities[i].Key);
                    }
                }));
                History.MarkAsSaved();   
            }
        }
        
        public async Task TryFind(DatabaseKey key)
        {
            var condition = $"`{tableDefinition.GroupByKeys[0]}` < {key[0]}";

            for (int i = 1; i < key.Count; ++i)
            {
                var sb = new StringBuilder();
                for (int j = 0; j < i; ++j)
                {
                    sb.Append($"`{tableDefinition.GroupByKeys[j]}` = {key[j]} AND ");
                }
                sb.Append($"`{tableDefinition.GroupByKeys[i]}` < {key[i]}");
                condition = $"({condition}) OR ({sb.ToString()})";
            }

            var where = Queries.Table(tableDefinition.TableName)
                .Where(r => r.Raw<bool>(condition));

            if (!string.IsNullOrEmpty(FilterViewModel.BuildWhere()))
                where = where.Where(r => r.Raw<bool>(FilterViewModel.BuildWhere()));

            var query = where.Select("COUNT(*) AS C");

            var result = await mySqlExecutor.ExecuteSelectSql(query.QueryString);

            var count = result[0]["C"];
            var offset = Convert.ToInt64(count.Item2);
            var modifiedOffset = Math.Max(0, offset - LimitQuery + 1);
            OffsetQuery = modifiedOffset;
            await ScheduleLoading();
            mainThread.Delay(() => SelectedRowIndex = (int)(offset - modifiedOffset), TimeSpan.FromMilliseconds(1));
        }
    }
}