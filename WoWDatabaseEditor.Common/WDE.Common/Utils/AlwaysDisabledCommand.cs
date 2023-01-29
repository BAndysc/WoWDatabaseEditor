using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;

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
    
    public class AlwaysDisabledAsyncCommand : IAsyncCommand
    {
        public static AlwaysDisabledAsyncCommand Command => new();

        public bool CanExecute(object? parameter)
        {
            return false;
        }

        public void Execute(object? parameter)
        {
        }

        public event EventHandler? CanExecuteChanged;

        public async Task ExecuteAsync() { }

        public void RaiseCanExecuteChanged()
        {
        }
    }
}