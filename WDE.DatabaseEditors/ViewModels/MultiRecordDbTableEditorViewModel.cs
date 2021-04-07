using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public class MultiRecordDbTableEditorViewModel : BindableBase, IDocument
    {
        private readonly DbEditorsSolutionItem solutionItem;
        private readonly Func<uint, Task<IDbTableData?>>? tableDataLoader;
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Lazy<IDbTableFieldFactory> fieldFactory;
        private readonly Lazy<IMySqlExecutor> sqlExecutor;

        private MultiRecordTableEditorHistoryHandler? historyHandler;
        
        public MultiRecordDbTableEditorViewModel(DbEditorsSolutionItem solutionItem, string tableName, 
            Func<uint, Task<IDbTableData?>>? tableDataLoader, Func<IHistoryManager> historyCreator,
            ITaskRunner taskRunner, Lazy<IItemFromListProvider> itemFromListProvider,
            Lazy<IDbTableFieldFactory> fieldFactory, Lazy<IMySqlExecutor> sqlExecutor)
        {
            this.solutionItem = solutionItem;
            this.tableDataLoader = tableDataLoader;
            this.itemFromListProvider = itemFromListProvider;
            this.fieldFactory = fieldFactory;
            this.sqlExecutor = sqlExecutor;

            // check if solution item already have cached data for table
            if (solutionItem.TableData != null)
                tableData = solutionItem.TableData as DbMultiRecordTableData;
            else
            {
                IsLoading = true;
                taskRunner.ScheduleTask($"Loading {tableName}..", LoadTableData);
            }

            Title = $"{tableName} Editor";

            OpenParameterWindow = new AsyncAutoCommand<ParameterValueHolder<long>?>(EditParameter);
            AddRow = new DelegateCommand(AddNewRow);
            DeleteRow = new DelegateCommand(DeleteExistingRow);
            Save = new DelegateCommand(SaveTable);
            SelectedRow = -1;
            
            History = historyCreator();
            SetupHistory();
        }

        private DbMultiRecordTableData? tableData;
        public DbMultiRecordTableData? TableData
        {
            get => tableData;
            set
            {
                tableData = value;
                RaisePropertyChanged(nameof(TableData));
            }
        }
        
        private bool isLoading;
        public bool IsLoading
        {
            get => isLoading;
            internal set => SetProperty(ref isLoading, value);
        }

        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;
        public AsyncAutoCommand<ParameterValueHolder<long>?> OpenParameterWindow { get; }
        public DelegateCommand AddRow { get; }
        public DelegateCommand DeleteRow { get; }
        public int? SelectedRow { get; set; }

        private async Task LoadTableData()
        {
            if (tableDataLoader == null)
            {
                IsLoading = false;
                return;
            }

            var data = await tableDataLoader.Invoke(solutionItem.Entry) as DbMultiRecordTableData;

            if (data == null)
                return;

            data.InitRows();
            SaveLoadedTableData(data);
        }

        private void SaveLoadedTableData(DbMultiRecordTableData data)
        {
            IsLoading = false;
            TableData = data;
            // for cache purpose
            solutionItem.CacheTableData(data);
            SetupHistory();
        }

        private void SaveTable()
        {
            if (tableData == null)
                return;

            var sql = MultiRecordTableSqlGenerator.GenerateSql(tableData);
            sqlExecutor.Value.ExecuteSql(sql);

            History.MarkAsSaved();
        }
        
        private async Task EditParameter(ParameterValueHolder<long>? valueHolder)
        {
            if (valueHolder == null)
                return;
            
            if (valueHolder.Parameter.HasItems)
            {
                var result = await itemFromListProvider.Value.GetItemFromList(valueHolder.Parameter.Items,
                    valueHolder.Parameter is FlagParameter, valueHolder.Value);
                if (result.HasValue)
                    valueHolder.Value = result.Value;
            }
        }

        private void AddNewRow()
        {
            tableData?.AddRow(fieldFactory.Value);
        }

        private void DeleteExistingRow()
        {
            if (SelectedRow.HasValue && SelectedRow.Value >= 0)
                tableData?.DeleteRow(SelectedRow.Value);
        }
        
        private void SetupHistory()
        {
            if (tableData == null)
                return;
            
            historyHandler = new MultiRecordTableEditorHistoryHandler(tableData);
            undoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            redoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            History.PropertyChanged += (sender, args) =>
            {
                undoCommand.RaiseCanExecuteChanged();
                redoCommand.RaiseCanExecuteChanged();
                IsModified = !History.IsSaved;
            };
            History.AddHandler(historyHandler);
        }

        public void Dispose()
        {
            historyHandler?.Dispose();
        }

        public string Title { get; }
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;

        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }
        public IHistoryManager History { get; }
    }
}