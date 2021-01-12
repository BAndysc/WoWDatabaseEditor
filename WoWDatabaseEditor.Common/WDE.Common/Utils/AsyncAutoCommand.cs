using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Annotations;

namespace WDE.Common.Utils
{
    public class AsyncAutoCommand : ICommand
    {
        private bool isBusy;

        private readonly AsyncCommand command;

        public AsyncAutoCommand([NotNull]
            Func<Task> execute,
            [CanBeNull]
            Func<object?, bool>? canExecute = null,
            [CanBeNull]
            Action<Exception>? onException = null,
            bool continueOnCapturedContext = false)
        {
            command = new AsyncCommand(async () =>
                {
                    IsBusy = true;
                    await execute();
                    IsBusy = false;
                },
                a => !isBusy && (canExecute?.Invoke(a) ?? true),
                e =>
                {
                    IsBusy = false;
                    onException?.Invoke(e);
                },
                continueOnCapturedContext);
        }

        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                isBusy = value;
                Application.Current.Dispatcher.Invoke(() => command.RaiseCanExecuteChanged());
            }
        }

        public bool CanExecute(object? parameter)
        {
            return command.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            ((ICommand) command).Execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add => command.CanExecuteChanged += value;
            remove => command.CanExecuteChanged -= value;
        }
    }
}