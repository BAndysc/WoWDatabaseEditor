using System;
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

namespace WDE.DatabaseEditors.ViewModels
{
    public class CreatureTemplateDbEditorViewModel: BindableBase, IDocument
    {
        private readonly IDbEditorTableDataProvider tableDataProvider;
        private readonly IWindowManager windowManager;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;
        private readonly Func<Task<uint?>> tableObjectSelector;
        private readonly Func<uint, Task<IDbTableData>> tableContentLoader;

        private readonly TemplateTableEditorHistoryHandler historyHandler;
        
        public CreatureTemplateDbEditorViewModel(IDbEditorTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider, IParameterFactory parameterFactory, 
            Func<Task<uint?>> tableObjectSelector, Func<uint, Task<IDbTableData>> tableContentLoader, Func<IHistoryManager> historyCreator, ITaskRunner taskRunner, IWindowManager windowManager)
        {
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
            this.tableObjectSelector = tableObjectSelector;
            this.tableContentLoader = tableContentLoader;
            this.windowManager = windowManager;

            OpenParameterWindow = new AsyncAutoCommand<IDbTableField?>(EditParameter);
            
            // setup history
            History = historyCreator();
            historyHandler = new TemplateTableEditorHistoryHandler();
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
            // load content
            taskRunner.ScheduleTask("Loading data for table...", LoadTable);
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
        
        public AsyncAutoCommand<IDbTableField?> OpenParameterWindow { get; }
        private readonly DelegateCommand undoCommand;
        private readonly DelegateCommand redoCommand;
        
        private async Task LoadTable()
        {
            var key = await tableObjectSelector.Invoke();
            if (key.HasValue)
            {
                var data = await tableContentLoader.Invoke(key.Value);
                TableData = data as DbTableData;
            }
        }

        private async Task EditParameter(IDbTableField? tableField)
        {
            if (tableField is DbTableField<long> longField)
            {
                var parameter = parameterFactory.Factory(tableField.ValueType);
                if (parameter != null)
                {
                    var result = await itemFromListProvider.GetItemFromList(parameter.Items, parameter is FlagParameter,
                        longField.Value);
                    if (result.HasValue)
                        longField.Value = result.Value;
                }
            }
        }
        
        public void Dispose()
        {
            historyHandler.Dispose();
        }

        public string Title { get; } = "Creature Template Editor";
        public ICommand Undo => undoCommand;
        public ICommand Redo => AlwaysDisabledCommand.Command;
        public ICommand Copy => redoCommand;
        public ICommand Cut => AlwaysDisabledCommand.Command;
        public ICommand Paste => AlwaysDisabledCommand.Command;
        public ICommand Save => AlwaysDisabledCommand.Command;
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