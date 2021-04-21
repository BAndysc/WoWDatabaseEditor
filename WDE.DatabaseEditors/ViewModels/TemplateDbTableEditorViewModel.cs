using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using DynamicData;
using Prism.Commands;
using Prism.Events;
using WDE.Common;
using WDE.Common.Events;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.MVVM;
using WDE.MVVM.Observable;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TemplateDbTableEditorViewModel : ObservableBase, ISolutionItemDocument
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IMessageBoxService messageBoxService;
        private readonly IParameterFactory parameterFactory;
        private readonly ISolutionTasksService solutionTasksService;

        private readonly DatabaseTableSolutionItem solutionItem;
        private readonly IDatabaseTableDataProvider tableDataProvider;

        public TemplateDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider,
            IHistoryManager history, ITaskRunner taskRunner, IMessageBoxService messageBoxService,
            IEventAggregator eventAggregator, ITableDefinitionProvider definitionProvider,
            IParameterFactory parameterFactory, ISolutionTasksService solutionTasksService,
            IQueryGenerator queryGenerator)
        {
            SolutionItem = solutionItem;
            this.itemFromListProvider = itemFromListProvider;
            this.solutionItem = solutionItem;
            this.tableDataProvider = tableDataProvider;
            this.messageBoxService = messageBoxService;
            this.parameterFactory = parameterFactory;
            this.solutionTasksService = solutionTasksService;
            History = history;
            tableData = null!;
            
            IsLoading = true;
            taskRunner.ScheduleTask($"Loading {solutionItem.TableId}..", LoadTableDefinition);
            
            Title = $"{solutionItem.TableId} Editor";

            undoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            redoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            OpenParameterWindow = new AsyncAutoCommand<DatabaseCellViewModel>(EditParameter);
            Save = new DelegateCommand(SaveSolutionItem);

            TableDefinition = definitionProvider.GetDefinition(solutionItem.TableId)!;
            
            CurrentFilter = FunctionalExtensions.Select<string, Func<DatabaseRowViewModel, bool>>(this.ToObservable(t => t.SearchText), text =>
                {
                    if (string.IsNullOrEmpty(text))
                        return item => true;
                    var lower = text.ToLower();
                    return item => item.Name.ToLower().Contains(lower);
                });

            AutoDispose(Rows.Connect()
                .Filter(CurrentFilter)
                .GroupOn(t => (t.CategoryName, t.CategoryIndex))
                .Transform(group => new DatabaseRowsGroupViewModel(group, GetGroupVisibility(group.GroupKey.CategoryName)))
                .DisposeMany()
                .FilterOnObservable(t => t.ShowGroup)
                .Sort(Comparer<DatabaseRowsGroupViewModel>.Create((x, y) => x.GroupOrder.CompareTo(y.GroupOrder)))
                .Bind(out ReadOnlyObservableCollection<DatabaseRowsGroupViewModel> filteredFields)
                .Subscribe(a =>
                {
                    
                }, b => throw b));
            FilteredRows = filteredFields;

            AutoDispose(eventAggregator.GetEvent<EventRequestGenerateSql>()
                .Subscribe(args =>
                {
                    if (args.Item is DatabaseTableSolutionItem dbEditItem)
                    {
                        if (solutionItem.Equals(dbEditItem))
                        {
                            args.Sql = queryGenerator.GenerateQuery(tableData);
                        }
                    }
                }));

            RevertCommand = new DelegateCommand<DatabaseCellViewModel>(view =>
            {
                if (!view.Parent.IsReadOnly)
                    view.ParameterValue.Revert();
            });
            SetNullCommand = new DelegateCommand<DatabaseCellViewModel>(view =>
            {
                if (view.CanBeNull && !view.Parent.IsReadOnly)
                    view.ParameterValue.SetNull();
            }, view => view.CanBeNull && !view.Parent.IsReadOnly);
        }
       
        private async Task EditParameter(DatabaseCellViewModel cell)
        {
            var valueHolder = cell.ParameterValue as ParameterValue<long>;
            
            if (valueHolder == null)
                return;

            if (valueHolder.Parameter.HasItems)
            {
                var result = await itemFromListProvider.GetItemFromList(valueHolder.Parameter.Items,
                    valueHolder.Parameter is FlagParameter, valueHolder.Value);
                if (result.HasValue)
                    valueHolder.Value = result.Value;
            }
        }

        private async Task LoadTableDefinition()
        {
            var data = await tableDataProvider.Load(solutionItem.TableId, solutionItem.Entries.ToArray()) as DatabaseTableData;

            if (data == null)
            {
                await messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to load data from database!")
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
                return;
            }
            
            if (solutionItem.OriginalValues != null)
            {
                foreach (var entity in data.Entities)
                {
                    var key = entity.GetCell(data.TableDefinition.TablePrimaryKeyColumnName);
                    if (key == null || key is not DatabaseField<long> longKey)
                        continue;
                    if (solutionItem.OriginalValues.TryGetValue((uint) longKey.Current.Value, out var originals))
                    {
                        foreach (var original in originals)
                        {
                            var cell = entity.GetCell(original.ColumnName);
                            if (cell == null)
                                continue;
                            cell.OriginalValue = original.OriginalValue;
                        }
                    }
                }
            }

            TableData = data;
            SetupHistory();
            IsLoading = false;
        }

        private void SetupHistory()
        {
            var historyHandler = AutoDispose(new TemplateTableEditorHistoryHandler(this));
            History.PropertyChanged += (sender, args) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(nameof(IsModified));
            };
            History.AddHandler(historyHandler);
        }

        private Dictionary<uint, List<EntityModifiedField>> GetModifiedFields()
        {
            var dict = new Dictionary<uint, List<EntityModifiedField>>();
            
            foreach (var entity in tableData.Entities)
            {
                var modified = entity.Fields.Where(f => f.IsModified).ToList();
                if (modified.Count == 0)
                    continue;

                var keyField = entity.GetCell(tableData.TableDefinition.TablePrimaryKeyColumnName);
                
                if (keyField == null || keyField is not DatabaseField<long> keyLong)
                    continue;

                var key = keyLong.Current.Value;
                dict[(uint) key] = modified.Select(f => new EntityModifiedField()
                    {ColumnName = f.FieldName, OriginalValue = f.OriginalValue}).ToList();
            }
        
            return dict;
        }

        private void SaveSolutionItem()
        {
            solutionItem.OriginalValues = GetModifiedFields();
            solutionTasksService.SaveSolutionToDatabaseTask(solutionItem);
            History.MarkAsSaved();
        }
        
        public ObservableCollection<DatabaseEntity> Entities { get; } = new();
        public ObservableCollection<string> Header { get; } = new();
        public ReadOnlyObservableCollection<DatabaseRowsGroupViewModel> FilteredRows { get; }
        public IObservable<Func<DatabaseRowViewModel, bool>> CurrentFilter { get; }
        public SourceList<DatabaseRowViewModel> Rows { get; } = new();

        public DatabaseTableDefinitionJson TableDefinition { get; }
        private DatabaseTableData tableData;
        public DatabaseTableData TableData
        {
            get => tableData;
            set
            {
                foreach (var group in TableDefinition.Groups)
                {
                    groupVisibilityByName.Add(group.Name, new ReactiveProperty<bool>(true));
                }
                
                tableData = value;
                RaisePropertyChanged(nameof(TableData));

                int categoryIndex = 0;
                int columnIndex = 0;
                Entities.AddRange(tableData.Entities);
                foreach (var entity in tableData.Entities)
                {
                    Header.Add(entity.GetCell(tableData.TableDefinition.TableNameSource)?.ToString() ?? "???");
                }
                foreach (var group in TableDefinition.Groups)
                {
                    categoryIndex++;
                    groupVisibilityByName[group.Name] = new ReactiveProperty<bool>(true);

                    foreach (var column in group.Fields)
                    {
                        var row = new DatabaseRowViewModel(column, group.Name, categoryIndex, columnIndex++);
                        foreach (var entity in tableData.Entities)
                        {
                            var cell = entity.GetCell(column.DbColumnName);
                            if (cell == null)
                                throw new Exception("this should never happen");
                            
                            IParameterValue parameterValue = null!;
                            if (cell is DatabaseField<long> longParam)
                            {
                                parameterValue = new ParameterValue<long>(longParam.Current, longParam.Original, parameterFactory.Factory(column.ValueType));
                            }
                            else if (cell is DatabaseField<string> stringParam)
                            {
                                parameterValue = new ParameterValue<string>(stringParam.Current, stringParam.Original, StringParameter.Instance);
                            }
                            else if (cell is DatabaseField<float> floatParameter)
                            {
                                parameterValue = new ParameterValue<float>(floatParameter.Current, floatParameter.Original, FloatParameter.Instance);
                            }

                            IObservable<bool>? cellVisible = null;
                            if (group.ShowIf.HasValue)
                            {
                                var compareCell = entity.GetCell(group.ShowIf.Value.ColumnName);
                                if (compareCell != null && compareCell is DatabaseField<long> lField)
                                {
                                    var comparedValue = group.ShowIf.Value.Value;
                                    cellVisible = Observable.Select(lField.Current.ToObservable(p => p.Value), val => val == comparedValue);
                                }
                            }

                            var cellViewModel = new DatabaseCellViewModel(row, cell, parameterValue, cellVisible);
                            row.Cells.Add(cellViewModel);
                        }
                        Rows.Add(row);
                    }
                }

                foreach (var e in tableData.Entities)
                {
                    var cell = e.GetCell("type");
                    if (cell == null)
                        continue;
                    cell.PropertyChanged += (_, _) => ReEvalVisibility();
                }
                ReEvalVisibility();
            }
        }

        private void ReEvalVisibility()
        {
            foreach (var group in TableDefinition.Groups)
            {
                if (!group.ShowIf.HasValue)
                    continue;

                groupVisibilityByName[group.Name].Value = false;
                foreach (var entity in tableData.Entities)
                {
                    var cell = entity.GetCell(group.ShowIf.Value.ColumnName);
                    if (cell is not DatabaseField<long> lField)
                        continue;
                    if (lField.Current.Value == group.ShowIf.Value.Value)
                    {
                        groupVisibilityByName[group.Name].Value = true;
                        break;
                    }
                }
            }
        }

        private Dictionary<string, ReactiveProperty<bool>> groupVisibilityByName = new();

        public IObservable<bool> GetGroupVisibility(string str)
        {
            return groupVisibilityByName[str];
        }
        
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }
        
        public DelegateCommand<DatabaseCellViewModel> RevertCommand { get; }
        public DelegateCommand<DatabaseCellViewModel> SetNullCommand { get; }
        public AsyncAutoCommand<DatabaseCellViewModel> OpenParameterWindow { get; }
        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;
        
        private string searchText = "";
        public string SearchText
        {
            get => searchText;
            set => SetProperty(ref searchText, value);
        }
        
        public string Title { get; }
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified => !History.IsSaved;
        public IHistoryManager History { get; }
        public ISolutionItem SolutionItem { get; }
    }

}