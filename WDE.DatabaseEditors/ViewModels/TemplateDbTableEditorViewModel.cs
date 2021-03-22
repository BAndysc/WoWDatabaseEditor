using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.History;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.Solution;
using WDE.Parameters.Models;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TemplateDbTableEditorViewModel: BindableBase, IDocument
    {
        private readonly Lazy<IItemFromListProvider> itemFromListProvider;
        private readonly Func<uint, Task<IDbTableData?>>? tableDataLoader;
        private readonly Lazy<IMessageBoxService> messageBoxService;
        private readonly IDbFieldNameSwapDataManager nameSwapDataManager;

        private TemplateTableEditorHistoryHandler? historyHandler;
        private readonly DbEditorsSolutionItem solutionItem;
        private DbTableFieldNameSwapHandler? tableFieldNameSwapHandler;
        
        public TemplateDbTableEditorViewModel(DbEditorsSolutionItem solutionItem, string tableName,
            Func<uint, Task<IDbTableData?>>? tableDataLoader, Lazy<IItemFromListProvider> itemFromListProvider, 
            Func<IHistoryManager> historyCreator, ITaskRunner taskRunner, Lazy<IMessageBoxService> messageBoxService,
            IDbFieldNameSwapDataManager nameSwapDataManager)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.solutionItem = solutionItem;
            this.tableDataLoader = tableDataLoader;
            this.messageBoxService = messageBoxService;
            this.nameSwapDataManager = nameSwapDataManager;

            if (solutionItem.TableData != null)
                tableData = solutionItem.TableData as DbTableData;
            else
            {
                IsLoading = true;
                taskRunner.ScheduleTask($"Loading {tableName}..", LoadTableDefinition);
            }

            Title = $"{tableName} Editor";
            
            OpenParameterWindow = new AsyncAutoCommand<ParameterValueHolder<long>?>(EditParameter);
            saveModifiedFields = new DelegateCommand(SaveSolutionItem);
            
            // setup history
            History = historyCreator();
            SetupHistory();
            SetupSwapDataHandler();
        }

        private DbTableData? tableData;
        public DbTableData? TableData
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
        
        public AsyncAutoCommand<ParameterValueHolder<long>?> OpenParameterWindow { get; }
        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;
        private readonly DelegateCommand saveModifiedFields;

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

        private async Task LoadTableDefinition()
        {
            if (tableDataLoader == null)
            {
                IsLoading = false;
                return;
            }
            
            var data = await tableDataLoader.Invoke(solutionItem.Entry) as DbTableData;

            if (data == null)
            {
                var result = await messageBoxService.Value.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Error!")
                    .SetMainInstruction($"Editor failed to load data from database!")
                    .SetIcon(MessageBoxIcon.Error)
                    .WithOkButton(true)
                    .Build());
                return;
            }

            if (solutionItem.ModifiedFields == null)
            {
                SaveLoadedTableData(data);
                return;
            }

            foreach (var field in data.Categories.SelectMany(c => c.Fields))
            {
                if (!solutionItem.ModifiedFields.ContainsKey(field.DbFieldName))
                    continue;
                    
                if (field is IStateRestorableField restorableField)
                    restorableField.RestoreLoadedFieldState(solutionItem.ModifiedFields[field.DbFieldName]);
            }

            SaveLoadedTableData(data);
        }

        private void SaveLoadedTableData(DbTableData data)
        {
            IsLoading = false;
            TableData = data;
            // for cache purpose
            solutionItem.CacheTableData(data);
            SetupHistory();
            SetupSwapDataHandler();
        }

        private void SaveSolutionItem()
        {
            if (tableData == null)
                return;

            var dict = new Dictionary<string, DbTableSolutionItemModifiedField>();
            
            foreach (var field in tableData.Categories.SelectMany(c => c.Fields).Where(f => f.IsModified))
            {
                if (field is IStateRestorableField restorableField)
                    dict[field.DbFieldName] = new(field.DbFieldName, 
                        restorableField.GetOriginalValueForPersistence(), 
                        restorableField.GetValueForPersistence());
            }

            solutionItem.ModifiedFields = dict;
            History.MarkAsSaved();
        }
        
        public void Dispose()
        {
            historyHandler?.Dispose();
            tableFieldNameSwapHandler?.Dispose();
        }

        private void SetupHistory()
        {
            if (tableData == null)
                return;
            
            historyHandler = new TemplateTableEditorHistoryHandler(tableData);
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

        private void SetupSwapDataHandler()
        {
            if (tableData == null)
                return;
            
            var swapData = nameSwapDataManager.GetSwapData(tableData.TableName);
            if (swapData.HasValue)
                tableFieldNameSwapHandler = new DbTableFieldNameSwapHandler(tableData, swapData.Value);
        }

        public string Title { get; }
        public ICommand Undo => undoCommand;
        public ICommand Redo => redoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => saveModifiedFields;
        public IAsyncCommand CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        private bool isModified;
        public bool IsModified
        {
            get => isModified;
            private set => SetProperty(ref isModified, value);
        }
        public IHistoryManager History { get; }
    }
}