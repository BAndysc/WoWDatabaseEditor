using System;
using System.Windows.Input;

namespace WDE.Common.Utils
{
    public class AlwaysDisabledCommand : ICommand
    {
        public static AlwaysDisabledCommand Command => new AlwaysDisabledCommand();
        
        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter) {}

        public event EventHandler? CanExecuteChanged;
    }
}