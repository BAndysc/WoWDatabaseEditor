using System;
using System.Windows.Input;
using Microsoft.Win32;
using Prism.Mvvm;
using Prism.Commands;
using WDE.Common.History;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.Conditions.Data;
using WDE.Conditions.History;

namespace WDE.Conditions.ViewModels
{
    public class ConditionsDefinitionEditorViewModel: BindableBase, IDocument
    {
        private readonly ConditionsEditorHistoryHandler historyHandler;
        
        public ConditionsDefinitionEditorViewModel(Func<IHistoryManager> historyCreator)
        {
            
            // history setup
            historyHandler = new ConditionsEditorHistoryHandler();
            History = historyCreator();
            UndoCommand = new DelegateCommand(History.Undo, () => History.CanUndo);
            RedoCommand = new DelegateCommand(History.Redo, () => History.CanRedo);
            History.PropertyChanged += (sender, args) =>
            {
                UndoCommand.RaiseCanExecuteChanged();
                RedoCommand.RaiseCanExecuteChanged();
                IsModified = !History.IsSaved;
            };
            History.AddHandler(historyHandler);
        }

        private DelegateCommand UndoCommand;
        private DelegateCommand RedoCommand;
        
        public void Dispose()
        {
            
        }

        public string Title { get; }
        public ICommand Undo => UndoCommand;
        public ICommand Redo => RedoCommand;
        public ICommand Copy { get; } = AlwaysDisabledCommand.Command;
        public ICommand Cut { get; } = AlwaysDisabledCommand.Command;
        public ICommand Paste { get; } = AlwaysDisabledCommand.Command;
        public ICommand Save { get; }
        public ICommand CloseCommand { get; set; } = AlwaysDisabledCommand.Command;
        public bool CanClose { get; } = true;
        private bool isModified;

        public bool IsModified
        {
            get => isModified;
            set
            {
                isModified = value;
                RaisePropertyChanged(nameof(IsModified));
            }
        }
        public IHistoryManager History { get; }
    }
}