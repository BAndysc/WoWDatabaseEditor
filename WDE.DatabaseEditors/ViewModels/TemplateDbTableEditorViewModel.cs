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
using WDE.DatabaseEditors.Solution;

namespace WDE.DatabaseEditors.ViewModels
{
    public class TemplateDbTableEditorViewModel: BindableBase, IDocument
    {
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;

        private readonly TemplateTableEditorHistoryHandler historyHandler;
        
        public TemplateDbTableEditorViewModel(DbEditorsSolutionItem solutionItem, IItemFromListProvider itemFromListProvider,
            IParameterFactory parameterFactory, Func<IHistoryManager> historyCreator)
        {
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
            tableData = solutionItem.TableData;

            OpenParameterWindow = new AsyncAutoCommand<IDbTableField?>(EditParameter);
            
            // setup history
            History = historyCreator();
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
        public ICommand Redo => redoCommand;
        public ICommand Copy => AlwaysDisabledCommand.Command;
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