using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.Events;
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
using WDE.DatabaseEditors.History.SingleRow;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.Utils;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.ViewModels.SingleRow
{
    public partial class SingleRowDbTableEditorViewModel : ViewModelBase, IBeforeSaveConfirmDocument
    {
        private readonly IDatabaseTableModelGenerator modelGenerator;
        private readonly IConditionEditService conditionEditService;
        private readonly IDatabaseEditorsSettings editorSettings;
        private readonly ITableEditorPickerService tableEditorPickerService;
        private readonly IMainThread mainThread;
        private readonly IPersonalGuidRangeService personalGuidRangeService;
        private readonly IMetaColumnsSupportService metaColumnsSupportService;
        private readonly ITablePersonalSettings personalSettings;
        private readonly DocumentMode mode;
        private readonly IDatabaseTableDataProvider tableDataProvider;

        private HashSet<DatabaseKey> keys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> removedKeys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> forceInsertKeys = new HashSet<DatabaseKey>();
        private HashSet<DatabaseKey> forceDeleteKeys = new HashSet<DatabaseKey>();
        private HashSet<(DatabaseKey key, ColumnFullName columnName)> forceUpdateCells = new HashSet<(DatabaseKey, ColumnFullName)>();
        private List<IDisposable> rowsDisposable = new();
        public ObservableCollection<DatabaseEntityViewModel> Rows { get; } = new();
        public IReadOnlyList<ITableRowGroup> OnlyGroup { get; }

        private bool showOnlyModified;
        [AlsoNotify(nameof(FocusedEntity))] [AlsoNotify(nameof(FocusedRow))] [Notify] private VerticalCursor focusedRowIndex;
        [AlsoNotify(nameof(FocusedCell))] [Notify] private int focusedCellIndex = -1;
        public override DatabaseEntity? FocusedEntity => FocusedRow?.Entity;
        public DatabaseEntityViewModel? FocusedRow => focusedRowIndex.RowIndex >= 0 && focusedRowIndex.RowIndex < Rows.Count ? Rows[focusedRowIndex.RowIndex] : null;
        public SingleRecordDatabaseCellViewModel? FocusedCell => FocusedRow != null && focusedCellIndex >= 0 && focusedCellIndex < FocusedRow.Cells.Count ? FocusedRow.Cells[focusedCellIndex] : null;
        public override bool SupportsMultiSelect => true;
        public override IReadOnlyList<DatabaseEntity>? MultiSelectionEntities
            => MultiSelection.All().Select(idx => Rows[idx.RowIndex].Entity).ToList();
        [Notify] private string rowsSummaryText = "";
        [Notify] private uint limitQuery = 300;
        [Notify] private ulong offsetQuery;
        [Notify] private ulong allRows;
        [Notify] private string searchText = "";
        public override ICommand Copy { get; }
        public override ICommand Paste { get; }
        public override ICommand Cut { get; }

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
        public ITableMultiSelection MultiSelection { get; } = new TableMultiSelection();
        public MySqlFilterViewModel FilterViewModel { get; }

        public AsyncAutoCommand AddNewCommand { get; }
        public DelegateCommand<SingleRecordDatabaseCellViewModel?> RevertCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel?> EditConditionsCommand { get; }
        public DelegateCommand<SingleRecordDatabaseCellViewModel?> SetNullCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel?> DuplicateCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel> OpenParameterWindow { get; }
        
        public AsyncAutoCommand DeleteRowSelectedCommand { get; }
        public DelegateCommand SetNullSelectedCommand { get; }
        public DelegateCommand GenerateInsertQueryForSelectedCommand { get; }
        public AsyncAutoCommand DuplicateSelectedCommand { get; }
        public AsyncAutoCommand RevertSelectedCommand { get; }
        
        public AsyncAutoCommand PreviousDataPage { get; }
        public AsyncAutoCommand RefreshQuery { get; }
        public AsyncAutoCommand NextDataPage { get; }
        public DatabaseKey? DefaultPartialKey { get; set; }

        public SingleRowDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider,
            IHistoryManager history, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator, ISolutionManager solutionManager, 
            IParameterFactory parameterFactory, ISolutionTasksService solutionTasksService,
            ISolutionItemNameRegistry solutionItemName, IDatabaseQueryExecutor mySqlExecutor,
            IQueryGenerator queryGenerator, IDatabaseTableModelGenerator modelGenerator,
            ITableDefinitionProvider tableDefinitionProvider,
            IConditionEditService conditionEditService, ISolutionItemIconRegistry iconRegistry,
            ISessionService sessionService, IDatabaseEditorsSettings editorSettings,
            IDatabaseTableCommandService commandService,
            IParameterPickerService parameterPickerService,
            IStatusBar statusBar, ITableEditorPickerService tableEditorPickerService,
            IMainThread mainThread, IPersonalGuidRangeService personalGuidRangeService,
            IClipboardService clipboardService, IMetaColumnsSupportService metaColumnsSupportService,
            ITablePersonalSettings personalSettings,
            DocumentMode mode = DocumentMode.Editor)
            : base(history, solutionItem, solutionItemName, 
            solutionManager, solutionTasksService, eventAggregator, 
            queryGenerator, tableDataProvider, messageBoxService, taskRunner, parameterFactory,
            tableDefinitionProvider, itemFromListProvider, iconRegistry, sessionService, commandService,
            parameterPickerService, statusBar, mySqlExecutor)
        {
            this.solutionItem = solutionItem;
            this.tableDataProvider = tableDataProvider;
            this.modelGenerator = modelGenerator;
            this.conditionEditService = conditionEditService;
            this.editorSettings = editorSettings;
            this.tableEditorPickerService = tableEditorPickerService;
            this.mainThread = mainThread;
            this.personalGuidRangeService = personalGuidRangeService;
            this.metaColumnsSupportService = metaColumnsSupportService;
            this.personalSettings = personalSettings;
            this.mode = mode;

            splitMode = editorSettings.MultiRowSplitMode;

            OpenParameterWindow = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel>(EditParameter);
            RevertCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(cell =>
            {
                cell!.ParameterValue!.Revert();
            }, cell => cell != null && cell.CanBeReverted && (cell?.TableField?.IsModified ?? false));
            SetNullCommand = new DelegateCommand<SingleRecordDatabaseCellViewModel?>(SetToNull, vm => vm != null && vm.CanBeSetToNull);
            DuplicateCommand = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel?>(async cell => await Duplicate(cell!.ParentEntity), vm => vm != null);
            EditConditionsCommand = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel?>(EditConditions);
            AddNewCommand = new AsyncAutoCommand(AddNewEntity);

            SetNullSelectedCommand = new DelegateCommand(() =>
            {
                IDisposable? disp = null;
                if (MultiSelection.MoreThanOne)
                    disp = historyHandler!.BulkEdit("Bulk set null");
                foreach (var x in MultiSelection.AllReversed())
                    Rows[x.RowIndex].Cells[focusedCellIndex].ParameterValue?.SetNull();
                disp?.Dispose();
            }, () => FocusedCell != null && FocusedCell.CanBeSetToNull).ObservesProperty(() => FocusedCell);

            RevertSelectedCommand = new AsyncAutoCommand(async () =>
            {
                using (BulkAction("Bulk revert"))
                {
                    foreach (var x in MultiSelection.AllReversed())
                        if (Rows[x.RowIndex].Cells[focusedCellIndex].CanBeReverted)
                            Rows[x.RowIndex].Cells[focusedCellIndex].ParameterValue?.Revert();
                }
            }, () => FocusedCell != null && FocusedCell.CanBeReverted);
            AutoDispose(this.ToObservable(() => FocusedCell).Subscribe(_ => RevertSelectedCommand.RaiseCanExecuteChanged()));

            DeleteRowSelectedCommand = new AsyncAutoCommand(async () =>
            {
                using (BulkAction("Bulk delete"))
                {
                    foreach (var x in MultiSelection.AllReversed())
                        ForceRemoveEntity(Entities[x.RowIndex]);
                    MultiSelection.Clear();   
                }
            }, () => FocusedRow != null);
            AutoDispose(this.ToObservable(() => FocusedRow).Subscribe(_ => DeleteRowSelectedCommand.RaiseCanExecuteChanged()));
            
            DuplicateSelectedCommand = new AsyncAutoCommand(async () => await Duplicate(FocusedRow!.Entity), () => FocusedRow != null);
            AutoDispose(this.ToObservable(() => FocusedRow).Subscribe(_ => DuplicateSelectedCommand.RaiseCanExecuteChanged()));

            GenerateInsertQueryForSelectedCommand = new DelegateCommand(() =>
            {
                var selectedKeys = MultiSelection.All().Select(index => Entities[index.RowIndex].GenerateKey(TableDefinition)).ToList();
                var tableData = new DatabaseTableData(TableDefinition, Entities);
                var query = queryGenerator.GenerateInsertQuery(selectedKeys, tableData);
                var item = new MetaSolutionSQL(new JustQuerySolutionItem(query));
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
            }, () => !MultiSelection.Empty).ObservesProperty(() => FocusedRow);
            
            foreach (var key in solutionItem.Entries.Select(x => x.Key))
                keys.Add(key);
            
            AutoDispose(new ActionDisposable(() =>
            {
                rowsDisposable.ForEach(x => x.Dispose());
                rowsDisposable.Clear();
            }));

            PreviousDataPage = new AsyncAutoCommand(() =>
            {
                OffsetQuery = (ulong)Math.Max(0, (long)OffsetQuery - (long)LimitQuery);
                return ScheduleLoading();
            });
            NextDataPage = new AsyncAutoCommand(() =>
            {
                OffsetQuery = allRows <= 0 ? OffsetQuery + LimitQuery : Math.Min(allRows - 1, OffsetQuery + LimitQuery);
                return ScheduleLoading();
            });
            RefreshQuery = new AsyncAutoCommand(ScheduleLoading);

            Copy = new DelegateCommand(() =>
            {
                clipboardService.SetText(FocusedCell!.StringValue ?? "(null)");
            }, () => FocusedCell != null).ObservesProperty(()=>FocusedCell);

            Cut = new DelegateCommand(() =>
            {
                Copy.Execute(null);
                if (FocusedCell!.CanBeSetToNull)
                    FocusedCell!.ParameterValue?.SetNull();
                else if (FocusedCell!.ParameterValue is IParameterValue<long> longValue)
                    longValue.Value = 0;
                else
                    FocusedCell!.UpdateFromString("");
            }, () => FocusedCell != null).ObservesProperty(()=>FocusedCell);

            Paste = new AsyncAutoCommand(async () =>
            {
                if (await clipboardService.GetText() is { } text)
                {
                    using (BulkAction("Bulk paste"))
                    {
                        foreach (var index in MultiSelection.All())
                            Rows[index.RowIndex].Cells[focusedCellIndex].UpdateFromString(text);
                    }
                }
            }, () => FocusedCell != null);
            
            var definition = tableDefinitionProvider.GetDefinition(solutionItem.TableName);
            FilterViewModel = new MySqlFilterViewModel(definition, () =>
            {
                OffsetQuery = 0;
                return ScheduleLoading();
            }, parameterFactory, parameterPickerService);
            
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

            OnlyGroup = new List<ITableRowGroup>() { new FakeGroup(Rows) };
            
            KeyBindings.Add(new CommandKeyBinding(new DelegateCommand(() =>
            {
                for (int i = 0, count = Entities.Count; i < count; i++)
                {
                    MultiSelection.Add(new VerticalCursor(0, i));
                }
            }), "Ctrl+A"));
            
            ScheduleLoading();
        }

        private void DoUndoableAction(string actionName, Action undo, Action redo)
        {
            if (historyHandler == null)
                redo();
            else
                historyHandler.DoAction(new AnonymousHistoryAction(actionName, undo, redo));
        }

        private class FakeGroup : ITableRowGroup
        {
            public event Action<ITableRowGroup, ITableRow>? RowChanged;
            public event Action<ITableRowGroup>? RowsChanged;
            public IReadOnlyList<ITableRow> Rows { get; set; }

            public FakeGroup(ObservableCollection<DatabaseEntityViewModel> rows)
            {
                Rows = rows;
                rows.CollectionChanged += RowsOnCollectionChanged;
            }

            private void RowsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                RowsChanged?.Invoke(this);
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    foreach (ITableRow row in e.NewItems!)
                    {
                        row.Changed += RowOnChanged;
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    foreach (ITableRow row in e.OldItems!)
                    {
                        row.Changed -= RowOnChanged;
                    }
                }
                else
                    throw new Exception("Operation not supported, becuase there is not way to unbind for " + e.Action);
            }

            private void RowOnChanged(ITableRow obj)
            {
                RowChanged?.Invoke(this, obj);
            }
        }

        private IDisposable BulkAction(string text)
        {
            if (MultiSelection.MoreThanOne)
                return historyHandler!.BulkEdit(text);
            return Disposable.Empty;
        }

        public override DatabaseEntity AddRow(DatabaseKey key, int? index = null)
        {
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key, true);
            index ??= Entities.Count;
            DoUndoableAction("Add row", () => ForceRemoveEntity(freshEntity), () => ForceInsertEntity(freshEntity, index.Value));
            return freshEntity;
        }
        
        private async Task<bool> CheckIfKeyExistsAndWarn(DatabaseKey key, int excludeIndex)
        {
            if (await ContainsKey(key, excludeIndex))
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Primary key exists in the database")
                    .SetMainInstruction($"Primary key {key} already exists in the database.")
                    .SetContent("Thus it cannot be saved, because it would cause a conflict. Please change the primary key or remove this row in order to save (nothing has been saved)")
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
            var index = FocusedRow == null ? Entities.Count : focusedRowIndex.RowIndex + 1;
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key, true);
            if (DefaultPartialKey.HasValue)
            {
                for (int i = 0; i < DefaultPartialKey.Value.Count; ++i)
                {
                    freshEntity.SetTypedCellOrThrow(TableDefinition.PrimaryKey[i], DefaultPartialKey.Value[i]);
                }
            }
            await SetupPersonalGuidValue(freshEntity);
            DoUndoableAction("Add new row", () =>
            {
                ForceRemoveEntity(freshEntity);
            }, () =>
            {
                ForceInsertEntity(freshEntity, index);
            });
            MultiSelection.Clear();
            MultiSelection.Add(FocusedRowIndex);
        }
        
        private void SetToNull(SingleRecordDatabaseCellViewModel? view)
        {
            if (view != null && view.CanBeNull && !view.IsReadOnly) 
                view.ParameterValue?.SetNull();
        }

        private async Task Duplicate(DatabaseEntity entity)
        {
            var duplicate = entity.Clone(DatabaseKey.PhantomKey, false);
            await SetupPersonalGuidValue(duplicate);
            var index = Entities.IndexOf(entity);
            DoUndoableAction("Duplicate row", () =>
            {
                ForceRemoveEntity(duplicate);
            }, () =>
            {
                ForceInsertEntity(duplicate, index);
            });
        }

        private async Task EditConditions(SingleRecordDatabaseCellViewModel? view)
        {
            if (view == null)
                return;

            if (tableDefinition.Condition == null)
                return;

            var conditionList = view.ParentEntity.Conditions;

            var key = new IDatabaseProvider.ConditionKey(tableDefinition.Condition.SourceType);
            if (tableDefinition.Condition.SourceGroupColumn is {} sourceGroup)
                key = key.WithGroup(sourceGroup.Calculate((int)view.ParentEntity.GetTypedValueOrThrow<long>(sourceGroup.Name)));
            if (tableDefinition.Condition.SourceEntryColumn is { } sourceEntry)
                key = key.WithEntry((int)view.ParentEntity.GetTypedValueOrThrow<long>(sourceEntry));
            if (tableDefinition.Condition.SourceIdColumn is { } sourceId)
                key = key.WithId((int)view.ParentEntity.GetTypedValueOrThrow<long>(sourceId));

            var newConditions = await conditionEditService.EditConditions(key, conditionList);
            if (newConditions == null)
                return;

            view.ParentEntity.Conditions = newConditions.ToList();
            if (tableDefinition.Condition.SetColumn != null)
            {
                var hasColumn = view.ParentEntity.GetCell(tableDefinition.Condition.SetColumn.Value);
                if (hasColumn is DatabaseField<long> lf)
                    lf.Current.Value = view.ParentEntity.Conditions.Count > 0 ? 1 : 0;
            }
        }

        private Task EditParameter(SingleRecordDatabaseCellViewModel cell)
        {
            if (cell.ParameterValue != null)
                return EditParameter(cell.ParameterValue, cell.ParentEntity);
            return Task.CompletedTask;
        }

        // TODO
        protected override IReadOnlyList<DatabaseKey> GenerateKeys() => keys.ToList();
        protected override IReadOnlyList<DatabaseKey>? GenerateDeletedKeys() => GenerateDeletedKeys(false);
        protected IReadOnlyList<DatabaseKey> GenerateDeletedKeys(bool saveQuery)
        {
            return removedKeys.Count == 0 ? forceDeleteKeys.ToList() : removedKeys.Union(forceDeleteKeys).ToList();
        }

        protected override async Task<DatabaseTableData?> LoadData()
        {
            return await databaseTableDataProvider.Load(solutionItem.TableName, FilterViewModel.BuildWhere(), (long)offsetQuery, (int)limitQuery, showOnlyModified ? keys.ToArray() : null) as DatabaseTableData;
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
                {
                    if (!await SaveDataNow())
                        return false;
                }
            }
            beforeLoadSelectedRow = FocusedRow?.Entity?.GenerateKey(TableDefinition);
            // save existing changes
            FocusedRowIndex = VerticalCursor.None;
            UpdateSolutionItem();

            if (historyHandler != null)
            {
                historyHandler.Dispose();
                History.RemoveHandler(historyHandler);
                historyHandler = null;
            }

            return true;
        }

        private async Task<bool> SaveDataNow()
        {
            if (await ShallSavePreventClosing())
                return false;
            UpdateSolutionItem();
            solutionManager.Refresh(SolutionItem);
            await mySqlExecutor.ExecuteSql(tableDefinition, await GenerateQuery());
            await sessionService.UpdateQuery(this);
            MaterializePhantomEntities();
            History.MarkAsSaved();
            return true;
        }
        
        protected override async Task InternalLoadData(DatabaseTableData data)
        {
            History.Clear();
            rowsDisposable.ForEach(x => x.Dispose());
            rowsDisposable.Clear();
            Rows.RemoveAll();
            entities.Add(new CustomObservableCollection<DatabaseEntity>());
            if (columns.Count == 0)
            {
                columns = tableDefinition.Groups.SelectMany(g => g.Fields).ToList();
                Debug.Assert(Columns.Count == 0);
                if (mode == DocumentMode.PickRow)
                    columns.Insert(0, new DatabaseColumnJson(){Name="Pick", Meta = "picker", PreferredWidth = 30});
                Columns.AddRange(columns.Select(c =>
                {
                    var column = new DatabaseColumnHeaderViewModel(c);
                    column.Width = personalSettings.GetColumnWidth(TableDefinition.Id, column.ColumnIdForUi, c.PreferredWidth ?? 120);
                    column.IsVisible = personalSettings.IsColumnVisible(TableDefinition.Id, column.ColumnIdForUi);
                    return AutoDispose(column);
                }));
                Columns.Each((col, i) => AutoDispose(col.ToObservable(x => x.IsVisible)
                    .Subscribe(@is =>
                    {
                        if (@is)
                        {
                            if (HiddenColumns.Contains(i))
                            {
                                HiddenColumns.Remove(i);
                                personalSettings.UpdateVisibility(TableDefinition.Id, col.ColumnIdForUi, @is);
                            }
                        }
                        else
                        {
                            if (!HiddenColumns.Contains(i))
                            {
                                HiddenColumns.Add(i);
                                personalSettings.UpdateVisibility(TableDefinition.Id, col.ColumnIdForUi, @is);
                            }
                        }
                    })));
                Columns.Each(col => col.ToObservable(c => c.Width)
                    .Skip(1)
                    .Throttle(TimeSpan.FromMilliseconds(300))
                    .Subscribe(width =>
                    {
                        personalSettings.UpdateWidth(TableDefinition.Id, col.ColumnIdForUi, col.PreferredWidth ?? 100,  ((int)(width) / 5) * 5);
                    }));
            }

            AddEntitiesAndFocus(data.Entities);

            FocusedRowIndex = new VerticalCursor(0,
                beforeLoadSelectedRow.HasValue
                    ? Entities.IndexIf(e => e.GenerateKey(TableDefinition) == beforeLoadSelectedRow)
                    : (Entities.Count > 0 ? 0 : -1));
            
            AllRows = (ulong)await tableDataProvider.GetCount(tableDefinition.Id, FilterViewModel.BuildWhere(), showOnlyModified ? keys : null);
            var start = allRows == 0 ? 0 : offsetQuery + 1;
            RowsSummaryText = $"{start} - {(long)offsetQuery + data.Entities.Count} of {allRows} rows";
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
        
        public override IDisposable BulkEdit(string name) => historyHandler?.BulkEdit(name) ?? Disposable.Empty;

        private SingleRowTableEditorHistoryHandler? historyHandler;

        protected override void UpdateSolutionItem()
        {
            var previousData = solutionItem.Entries.Where(e => Entities.All(entity => entity.GenerateKey(TableDefinition) != e.Key));
            
            solutionItem.Entries = Entities.Where(e => (!e.ExistInDatabase || EntityIsModified(e)) && !e.Phantom)
                .Select(e => new SolutionItemDatabaseEntity(e.Key, e.ExistInDatabase, e.ConditionsModified, GetOriginalFields(e)))
                .Union(previousData)
                .ToList();
            solutionItem.DeletedEntries = removedKeys.ToList();
        }

        private bool EntityIsModified(DatabaseEntity databaseEntity)
        {
            // todo: optimize?
            return databaseEntity.ConditionsModified || databaseEntity.Cells.Any(c => c.Value.IsModified);
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
            
            entities[0].RemoveAt(indexOfEntity);
            if (FocusedRow?.Entity == entity)
                FocusedRowIndex = VerticalCursor.None;
            
            Rows[indexOfEntity].ChangedCell -= OnRowChangedCell;
            Rows[indexOfEntity].Changed -= OnRowChanged;
            Rows.RemoveAt(indexOfEntity);

            return true;
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
                    var label = Observable.Select(entity.ToObservable(e => e.Conditions), c => "Conditions (" + c.CountActualConditions() + ")");
                    cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, "Conditions", EditConditionsCommand, row, entity, label));
                }
                else if (column.IsMetaColumn)
                {
                    if (column.Meta!.StartsWith("expression:"))
                    {
                        throw new NotImplementedException();
                    }
                    else if (column.Meta!.StartsWith("customfield:"))
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        var (command, title) = metaColumnsSupportService.GenerateCommand(this, tableDefinition.DataDatabaseType, column.Meta!, entity, entity.GenerateKey(TableDefinition));
                        cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, column.Name, command, row, entity, title));
                    }
                }
                else
                {
                    var cell = entity.GetCell(column.DbColumnFullName);
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
            
            entities[0].Insert(index, entity);
            Rows.Insert(index, row);
            focusedRowIndex = VerticalCursor.None;
            FocusedRowIndex = new VerticalCursor(0, index);
            return true;
        }

        private void OnRowChangedCell(DatabaseEntityViewModel entity, SingleRecordDatabaseCellViewModel cell, ColumnFullName columnName)
        {
            if (History.IsUndoing)
                return;

            if (columnName.ForeignTable == null && tableDefinition.GroupByKeys.Contains(columnName) && !entity.IsPhantomEntity)
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
            
            int indexOfRow = Rows.IndexOf(entity);
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

        private async Task<bool> ContainsKey(DatabaseKey key, int excludeIndex = -1)
        {
            if (Rows.Select((row, index) => (row, index)).Any(pair => pair.row.Entity.GenerateKey(TableDefinition) == key && pair.index != excludeIndex))
                return true;

            if (removedKeys.Contains(key))
                return false;

            return await databaseTableDataProvider.GetCount(tableDefinition.Id, null, new [] { key }) > 0;
        }
        
        private void AddEntitiesAndFocus(IReadOnlyList<DatabaseEntity> tableDataEntities)
        {
            foreach (var entity in tableDataEntities)
                ForceInsertEntity(entity, Entities.Count);
            focusedRowIndex = VerticalCursor.None;
            if (Rows.Count > 0)
                FocusedRowIndex = new VerticalCursor(0, 0);
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
                    return Queries.Raw(DataDatabaseType.World, "ERROR");
                solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);
            }

            data = new DatabaseTableData(tableDefinition, data == null ? newData : data.Entities.Union(newData).ToList());
            
            var query = queryGenerator.GenerateQuery(GenerateKeys(), GenerateDeletedKeys(saveQuery), data);

            if (!saveQuery)
                return query;

            IMultiQuery multi = Queries.BeginTransaction(tableDefinition.DataDatabaseType);
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
        
        public async Task<bool> ShallSavePreventClosing()
        {
            for (var index = 0; index < Entities.Count; index++)
            {
                var entity = Entities[index];
                if (!entity.Phantom)
                    continue;

                if (!await CheckIfKeyExistsAndWarn(entity.GenerateKey(TableDefinition), index))
                    return true;
            }

            return false;    
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
        
        public async Task TryFind(DatabaseKey key, string? customWhere = null)
        {
            var condition = $"`{tableDefinition.GroupByKeys[0]}` < {key[0]}";

            if (key.Count != tableDefinition.GroupByKeys.Count)
            {
                var expected = string.Join(", ", tableDefinition.GroupByKeys);
                throw new Exception($"Key count does not match group by count! The key is: {key}. Expected columns: ({expected})");
            }
            
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

            var where = Queries.Table(tableDefinition.Id)
                .Where(r => r.Raw<bool>(condition));

            if (customWhere != null)
            {
                FilterViewModel.FilterText = customWhere;
                FilterViewModel.SelectedColumn = FilterViewModel.RawSqlColumn;
            }
            
            if (!string.IsNullOrEmpty(FilterViewModel.BuildWhere()))
                where = where.Where(r => r.Raw<bool>(FilterViewModel.BuildWhere()));

            var query = where.Select("COUNT(*) AS C");

            var result = await mySqlExecutor.ExecuteSelectSql(tableDefinition, query.QueryString);
            
            var count = result.Rows == 0 ? 0L : result.Value(0, result.ColumnIndex("C"));
            var offset = Convert.ToInt64(count);
            var modifiedOffset = (ulong)Math.Max(0, offset - LimitQuery / 2 + 1);
            OffsetQuery = modifiedOffset;
            await ScheduleLoading();
            mainThread.Delay(() =>
            {
                FocusedRowIndex = new VerticalCursor(0, (int)((ulong)offset - modifiedOffset));
                MultiSelection.Clear();
                MultiSelection.Add(FocusedRowIndex);
            }, TimeSpan.FromMilliseconds(1));
        }

        public void UpdateSelectedCells(string text)
        {
            System.IDisposable disposable = new EmptyDisposable();
            if (MultiSelection.MoreThanOne)
                disposable = historyHandler!.BulkEdit("Bulk edit property");
            foreach (var selected in MultiSelection.All())
                Rows[selected.RowIndex].CellsList[focusedCellIndex].UpdateFromString(text);
            disposable.Dispose();
        }
    }
}