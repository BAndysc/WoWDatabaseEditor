using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using AvaloniaStyles.Controls.FastTableView;
using Newtonsoft.Json;
using Prism.Commands;
using Prism.Events;
using PropertyChanged.SourceGenerator;
using WDE.Common;
using WDE.Common.Avalonia;
using WDE.Common.Database;
using WDE.Common.Disposables;
using WDE.Common.History;
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
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Services;
using WDE.DatabaseEditors.Solution;
using WDE.DatabaseEditors.ViewModels.MultiRow;
using WDE.MVVM;

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
        private readonly IDatabaseTableDataProvider tableDataProvider;

        private HashSet<uint> keys = new HashSet<uint>();
        private HashSet<uint> removedKeys = new HashSet<uint>();
        private List<System.IDisposable> rowsDisposable = new();
        public ObservableCollection<DatabaseEntityViewModel> Rows { get; } = new();

        [AlsoNotify(nameof(SelectedRow))] [Notify] private int selectedRowIndex;
        [AlsoNotify(nameof(SelectedCell))] [Notify] private int selectedCellIndex;
        public DatabaseEntityViewModel? SelectedRow => selectedRowIndex >= 0 && selectedRowIndex < Rows.Count ? Rows[selectedRowIndex] : null;
        public SingleRecordDatabaseCellViewModel? SelectedCell => SelectedRow != null && selectedCellIndex >= 0 && selectedCellIndex < SelectedRow.Cells.Count ? SelectedRow.Cells[selectedCellIndex] : null;
        [Notify] private string rowsSummaryText = "";
        [Notify] private int limitQuery = 300;
        [Notify] private long offsetQuery = 0;

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
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel?> RemoveTemplateCommand { get; }
        public AsyncAutoCommand<SingleRecordDatabaseCellViewModel?> RevertCommand { get; }
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

        public event Action<DatabaseEntity>? OnDeletedQuery;
        public event Action<DatabaseEntity>? OnKeyDeleted;
        public event Action<uint>? OnKeyAdded;

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
            IParameterPickerService parameterPickerService) 
            : base(history, solutionItem, solutionItemName, 
            solutionManager, solutionTasksService, eventAggregator, 
            queryGenerator, tableDataProvider, messageBoxService, taskRunner, parameterFactory,
            tableDefinitionProvider, itemFromListProvider, iconRegistry, sessionService, commandService,
            parameterPickerService)
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

            splitMode = editorSettings.MultiRowSplitMode;

            OpenParameterWindow = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel>(EditParameter);
            RemoveTemplateCommand = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel?>(RemoveTemplate, vm => vm != null);
            RevertCommand = new AsyncAutoCommand<SingleRecordDatabaseCellViewModel?>(Revert, cell => cell is SingleRecordDatabaseCellViewModel vm && vm.CanBeReverted && (vm.TableField?.IsModified ?? false));
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
                await Revert(SelectedCell);
            }, () => SelectedCell != null && SelectedCell.CanBeReverted);
            AutoDispose(this.ToObservable(() => SelectedCell).Subscribe(_ => RevertSelectedCommand.RaiseCanExecuteChanged()));

            DeleteRowSelectedCommand = new AsyncAutoCommand(async () =>
            {
                await RemoveEntity(SelectedRow!.Entity);
            }, () => SelectedRow != null);
            AutoDispose(this.ToObservable(() => SelectedRow).Subscribe(_ => DeleteRowSelectedCommand.RaiseCanExecuteChanged()));
            
            DuplicateSelectedCommand = new AsyncAutoCommand(async () =>
            {
                var duplicate = SelectedRow!.Entity.Clone();
                //todo ask for a new key
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
            
            var definition = tableDefinitionProvider.GetDefinition(solutionItem.DefinitionId);
            FilterViewModel = new MySqlFilterViewModel(definition, ScheduleLoading);
            
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

        public override DatabaseEntity AddRow(uint key)
        {
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key);
            ForceInsertEntity(freshEntity, Entities.Count);
            return freshEntity;
        }
        
        private async Task AddNewEntity()
        {
            var parameter = parameterFactory.Factory(tableDefinition.Picker);
            var selected = await itemFromListProvider.GetItemFromList(parameter.Items, false);
            if (!selected.HasValue)
                return;

            uint key = (uint) selected;

            if (ContainsKey(key))
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Key already added")
                    .SetMainInstruction($"Key {key} is already added to this editor")
                    .SetContent("You cannot add it again, just edit existing.")
                    .WithOkButton(true)
                    .SetIcon(MessageBoxIcon.Error)
                    .Build());
                return;
            }
            
            OnKeyAdded?.Invoke(key);
            
            var freshEntity = modelGenerator.CreateEmptyEntity(tableDefinition, key);
            ForceInsertEntity(freshEntity, SelectedRow == null ? Entities.Count : selectedRowIndex + 1);
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
        
        private async Task Revert(SingleRecordDatabaseCellViewModel? view)
        {
            if (view == null || view.IsReadOnly || view.TableField == null)
                return;
            
            view.ParameterValue!.Revert();
            
            if (!view.ParentEntity.ExistInDatabase)
                return;
            
            if (!mySqlExecutor.IsConnected)
                return;

            if (!await messageBoxService.ShowDialog(new MessageBoxFactory<bool>()
                    .SetTitle("Reverting")
                    .SetMainInstruction("Do you want to revert field in the database?")
                    .SetContent(
                        "Reverted field will become unmodified field and unmodified fields are not generated in query. Therefore if you want to revert the field in the database, it can be done now.\n\nDo you want to revert the field in the database now (this will execute query)?")
                    .SetIcon(MessageBoxIcon.Information)
                    .WithYesButton(true)
                    .WithNoButton(false)
                    .Build()))
                return;

            var query = queryGenerator.GenerateUpdateFieldQuery(tableDefinition, view.ParentEntity, view.TableField);
            await mySqlExecutor.ExecuteSql(query);
        }

        private async Task RemoveTemplate(SingleRecordDatabaseCellViewModel? view)
        {
            if (view == null)
                return;

            await RemoveEntity(view.ParentEntity);
        }

        private Task EditParameter(SingleRecordDatabaseCellViewModel cell)
        {
            if (cell.ParameterValue != null)
                return EditParameter(cell.ParameterValue);
            return Task.CompletedTask;
        }

        // TODO
        protected override IReadOnlyList<uint> GenerateKeys() => keys.ToList();
        protected override IReadOnlyList<uint>? GenerateDeletedKeys() => removedKeys.Count == 0 ? null : removedKeys.ToList();
        protected override string? CustomWhere => FilterViewModel.BuildWhere();
        protected override int Limit => limitQuery;
        protected override long Offset => offsetQuery;

        protected override Task BeforeLoadData()
        {
            // save existing changes
            UpdateSolutionItem();
            return base.BeforeLoadData();
        }

        protected override async Task InternalLoadData(DatabaseTableData data)
        {
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
            
            var allRows = await tableDataProvider.GetCount(tableDefinition.Id, FilterViewModel.BuildWhere());
            RowsSummaryText = $"{offsetQuery + 1} - {offsetQuery + data.Entities.Count} of {allRows} rows";
            // TODO
            //historyHandler = History.AddHandler(AutoDispose(new MultiRowTableEditorHistoryHandler(this)));
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

        private MultiRowTableEditorHistoryHandler? historyHandler;

        protected override void UpdateSolutionItem()
        {
            var previousData = solutionItem.Entries.Where(e => Entities.All(entity => entity.Key != e.Key));
            
            solutionItem.Entries = Entities.Where(e => !e.ExistInDatabase || EntityIsModified(e))
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

        public async Task<bool> RemoveEntity(DatabaseEntity entity)
        {
            //Todo
            removedKeys.Add(entity.Key);
            Rows.RemoveIf(r => r.Entity == entity);
            Entities.Remove(entity);
            OnKeyDeleted?.Invoke(entity);
            return true;
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
            if (indexOfEntity == -1)
                return false;
            
            Entities.RemoveAt(indexOfEntity);
            if (SelectedRow?.Entity == entity)
                SelectedRowIndex = -1;
            Rows.RemoveIf(x => x.Entity == entity);

            return true;
        }
        
        public async Task<bool> AddEntity(DatabaseEntity entity)
        {
            return ForceInsertEntity(entity, Entities.Count);
        }

        public override bool ForceInsertEntity(DatabaseEntity entity, int index)
        {
            var pseudoItem = new DatabaseTableSolutionItem(tableDefinition.Id, tableDefinition.IgnoreEquality);
            var savedItem = sessionService.Find(pseudoItem);
            if (savedItem is DatabaseTableSolutionItem savedTableItem)
                savedTableItem.UpdateEntitiesWithOriginalValues(new List<DatabaseEntity>(){entity});

            var row = new DatabaseEntityViewModel(entity);
            row.Changed += OnRowChanged;
            AutoDisposeEntity(new ActionDisposable(() => row.Changed -= OnRowChanged));
            
            int columnIndex = 0;
            foreach (var column in columns)
            {
                SingleRecordDatabaseCellViewModel cellViewModel;

                if (column.IsConditionColumn)
                {
                    var label = entity.ToObservable(e => e.Conditions).Select(c => "Edit (" + (c?.Count ?? 0) + ")");
                    cellViewModel = AutoDisposeEntity(new SingleRecordDatabaseCellViewModel(columnIndex, "Conditions", EditConditionsCommand, row, entity, label));
                }
                else
                {
                    var cell = entity.GetCell(column.DbColumnName);
                    if (cell == null)
                        throw new Exception("this should never happen");

                    IParameterValue parameterValue = null!;
                    if (cell is DatabaseField<long> longParam)
                    {
                        parameterValue = new ParameterValue<long, DatabaseEntity>(entity, longParam.Current, longParam.Original, parameterFactory.Factory(column.ValueType));
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
            SelectedRowIndex = index;
            return true;
        }

        private void OnRowChanged(ITableRow obj)
        {
            if (obj is not DatabaseEntityViewModel row)
                return;

            if (EntityIsModified(row.Entity))
                keys.Add(row.Key);
            else
                keys.Remove(row.Key);
        }

        private bool ContainsKey(uint key)
        {
            return Rows.Any(row => row.Key == key);
        }
        
        private async Task AsyncAddEntities(IReadOnlyList<DatabaseEntity> tableDataEntities)
        {
            foreach (var entity in tableDataEntities)
            {
                await AddEntity(entity);
            }
        }

        public override async Task<IList<(ISolutionItem, string)>> GenerateSplitQuery()
        {
            var sql = await GenerateQuery();
            return new List<(ISolutionItem, string)>() { (solutionItem, sql) };
        }
        
        private T AutoDisposeEntity<T>(T entity) where T : IDisposable
        {
            rowsDisposable.Add(entity);
            return entity;
        }
        
        public override async Task<string> GenerateQuery()
        {
            var newData = Entities.Where(e => !e.ExistInDatabase || EntityIsModified(e)).ToList();

            var newDataKeys = newData.Select(e => e.Key).ToList();
            
            var keysString = string.Join(",", keys.Except(newDataKeys));
            IDatabaseTableData? data = null;
            if (!string.IsNullOrEmpty(keysString))
            {
                data = await databaseTableDataProvider.Load(tableDefinition.Id, $"`{tableDefinition.TableName}`.`{tableDefinition.TablePrimaryKeyColumnName}` IN ({keysString})", null, keys.Count);
                if (data == null)
                    return "ERROR";
                solutionItem.UpdateEntitiesWithOriginalValues(data.Entities);
            }

            data = new DatabaseTableData(tableDefinition, data == null ? newData : data.Entities.Union(newData).ToList());
            
            return queryGenerator
                .GenerateQuery(GenerateKeys(),  GenerateDeletedKeys(), data).QueryString;
        }
    }
}