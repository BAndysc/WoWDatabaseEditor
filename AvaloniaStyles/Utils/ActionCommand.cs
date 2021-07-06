using System;
using System.Windows.Input;

namespace AvaloniaStyles.Utils
{
    internal class ActionCommand : ICommand
    {
        private Action action;
        private Func<bool> canExecute;

        public ActionCommand(Action action, Func<bool> canExecute)
        {
            this.action = action;
            this.canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute();
        }

        public void Execute(object? parameter)
        {
            action();
        }

        public void InvokeCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(null, EventArgs.Empty);
        }

        public event EventHandler? CanExecuteChanged;
    }
}