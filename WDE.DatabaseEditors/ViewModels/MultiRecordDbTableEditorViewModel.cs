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
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Factories;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.DatabaseEditors.Solution;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    /*public class MultiRecordDbTableEditorViewModel : BindableBase, IDocument
    {
        private readonly DatabaseTableSolutionItem solutionItem;
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IDatabaseFieldFactory fieldFactory;
        private readonly IMySqlExecutor sqlExecutor;
        private readonly IMessageBoxService messageBoxService;

        private MultiRecordTableEditorHistoryHandler? historyHandler;
        
        public MultiRecordDbTableEditorViewModel(DatabaseTableSolutionItem solutionItem,
            IDatabaseTableDataProvider tableDataProvider, IHistoryManager history,
            ITaskRunner taskRunner, IItemFromListProvider itemFromListProvider,
            IDatabaseFieldFactory fieldFactory, IMySqlExecutor sqlExecutor,
            IMessageBoxService messageBoxService)
        {
            this.solutionItem = solutionItem;
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.fieldFactory = fieldFactory;
            this.sqlExecutor = sqlExecutor;
            this.messageBoxService = messageBoxService;

            History = history;
            IsLoading = true;
            taskRunner.ScheduleTask($"Loading {solutionItem.TableId}..", LoadTableData);
            
            undoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            redoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);

            Title = $"{solutionItem.TableId} Editor";

            OpenParameterWindow = new AsyncAutoCommand<ParameterValueHolder<long>?>(EditParameter);
            AddRow = new DelegateCommand(AddNewRow);
            DeleteRow = new DelegateCommand(DeleteExistingRow);
            Save = new DelegateCommand(SaveTable);
            SelectedRow = -1;
            
            SetupHistory();
        }

        private DatabaseMultiRecordTableData? tableData;
        public DatabaseMultiRecordTableData? TableData
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
            var data = await tableDataProvider.Load(solutionItem.TableId, solutionItem.Entry) as DatabaseMultiRecordTableData;

            if (data == null)
            {
                IsLoading = false;
                return;
            }

            data.InitRows();
            SaveLoadedTableData(data);
        }

        private void SaveLoadedTableData(DatabaseMultiRecordTableData data)
        {
            IsLoading = false;
            TableData = data;
            SetupHistory();
        }

        private void SaveTable()
        {
            if (tableData == null)
                return;

            var sql = MultiRecordTableSqlGenerator.GenerateSql(tableData);
            try
            {
                sqlExecutor.ExecuteSql(sql);
                History.MarkAsSaved();
            }
            catch (Exception e)
            {
                messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to save data to database!")
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
            }
        }
        
        private async Task EditParameter(ParameterValueHolder<long>? valueHolder)
        {
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

        private void AddNewRow()
        {
            tableData?.AddRow(fieldFactory);
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
        public IAsyncCommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;

        public bool IsModified
        {
            get => isModified;
            set => SetProperty(ref isModified, value);
        }
        public IHistoryManager History { get; }
    }*/
}