using System;
using System.ComponentModel;
using System.Windows.Input;
using WDE.Common.History;
using WDE.Common.Managers;

namespace WoWDatabaseEditor.ViewModels
{
    public class AboutViewModel : IDocument
    {
        public void Dispose()
        {
        }

        public string Title { get; } = "About";
        public ICommand Undo { get; } = new DisabledCommand();
        public ICommand Redo { get; } = new DisabledCommand();
        public ICommand Copy { get; } = new DisabledCommand();
        public ICommand Cut { get; } = new DisabledCommand();
        public ICommand Paste { get; } = new DisabledCommand();
        public ICommand Save { get; } = new DisabledCommand();
        public ICommand? CloseCommand { get; set; } = null;
        public bool CanClose { get; } = true;
        public bool IsModified { get; } = false;
        public IHistoryManager? History { get; } = null;
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class DisabledCommand : ICommand
    {
        public bool CanExecute(object? parameter)
        {
            return false;
        }

        public void Execute(object? parameter)
        {
            throw new NotImplementedException();
        }

        public event EventHandler? CanExecuteChanged;
    }
}