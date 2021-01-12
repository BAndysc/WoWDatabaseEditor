using System;
using System.Windows.Input;

namespace WDE.Common.Utils
{
    public class AlwaysDisabledCommand : ICommand
    {
        public static AlwaysDisabledCommand Command => new();

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