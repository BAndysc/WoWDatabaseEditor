using System;
using System.Windows.Controls;
using System.Windows.Input;
using WDE.Common.History;

namespace WDE.Common.Managers
{
    public sealed class Document : IDocument
    {
        public string Title { get; set; }
        public ICommand Undo { get; set; } = new DisabledCommand();
        public ICommand Redo { get; set; } = new DisabledCommand();
        public ICommand Save { get; set; }
        public ICommand Copy { get; set; } = new DisabledCommand();
        public ICommand Cut { get; set; } = new DisabledCommand();
        public ICommand Paste { get; set; } = new DisabledCommand();

        public ICommand CloseCommand { get; set; }
        public bool CanClose { get; set; }
        
        public IHistoryManager History { get; set; }
        
        public ContentControl Content { get; set; }

        private class DisabledCommand : ICommand
        {
            public bool CanExecute(object? parameter)
            {
                return false;
            }

            public void Execute(object? parameter)
            {
            }

            public event EventHandler? CanExecuteChanged;
        }
    }
}