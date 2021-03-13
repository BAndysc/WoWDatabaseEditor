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
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly Func<uint, Task<IDbTableData>>? tableDataLoader;

        private TemplateTableEditorHistoryHandler historyHandler;
        private readonly DbEditorsSolutionItem solutionItem;
        
        public TemplateDbTableEditorViewModel(DbEditorsSolutionItem solutionItem, IItemFromListProvider itemFromListProvider,
            Func<IHistoryManager> historyCreator, Func<uint, Task<IDbTableData>>? tableDataLoader, ITaskRunner taskRunner)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.solutionItem = solutionItem;
            this.tableDataLoader = tableDataLoader;
            if (solutionItem.TableData != null)
                tableData = solutionItem.TableData;
            else
                taskRunner.ScheduleTask($"Loading {solutionItem.TableName}..", LoadTableDefinition);

            Title = $"{solutionItem.TableName} Editor";
            
            OpenParameterWindow = new AsyncAutoCommand<object?>(EditParameter);
            saveModifiedFields = new DelegateCommand(SaveSolutionItem);
            
            // setup history
            History = historyCreator();
            SetupHistory();
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
        
        public AsyncAutoCommand<object?> OpenParameterWindow { get; }
        private DelegateCommand undoCommand;
        private DelegateCommand redoCommand;
        private readonly DelegateCommand saveModifiedFields;

        private async Task EditParameter(object? tableField)
        {
            if (tableField is ParameterValueHolder<long> valueHolder)
            {
                if (valueHolder.Parameter.HasItems)
                {
                    var result = await itemFromListProvider.GetItemFromList(valueHolder.Parameter.Items,
                        valueHolder.Parameter is FlagParameter, valueHolder.Value);
                    if (result.HasValue)
                        valueHolder.Value = result.Value;
                }
            }
        }

        private async Task LoadTableDefinition()
        {
            if (tableDataLoader == null)
                return;

            var data = await tableDataLoader.Invoke(solutionItem.Entry) as DbTableData;

            if (solutionItem.ModifiedFields == null)
            {
                TableData = data;
                SetupHistory();
                return;
            }

            foreach (var field in data.Categories.SelectMany(c => c.Fields))
            {
                if (!solutionItem.ModifiedFields.ContainsKey(field.FieldName))
                    continue;
                    
                if (field is IStateRestorableField restorableField)
                    restorableField.RestoreLoadedFieldState(solutionItem.ModifiedFields[field.FieldName]);
            }

            TableData = data;
            SetupHistory();
        }

        private void SaveSolutionItem()
        {
            if (tableData == null)
                return;

            var dict = new Dictionary<string, DbTableSolutionItemModifiedField>();
            
            foreach (var field in tableData.Categories.SelectMany(c => c.Fields).Where(f => f.IsModified))
            {
                if (field is IStateRestorableField restorableField)
                    dict[field.FieldName] = new(field.FieldName, restorableField.GetValueForPersistence());
            }

            solutionItem.ModifiedFields = dict;
            History.MarkAsSaved();
        }
        
        public void Dispose()
        {
            historyHandler.Dispose();
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
                RaisePropertyChanged(nameof(IsModified));
            };
            History.AddHandler(historyHandler);
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