using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Annotations;

namespace WDE.Common.Utils
{
    public class AsyncAutoCommand : ICommand
    {
        private bool _isBusy = false;

        private bool isBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                command.RaiseCanExecuteChanged();
            }
        }
        
        private AsyncCommand command;
        public AsyncAutoCommand([NotNull] Func<Task> execute,
            [CanBeNull] Func<object?, bool>? canExecute = null, 
            [CanBeNull] Action<Exception>? onException = null, 
            bool continueOnCapturedContext = false)
        {
            command = new AsyncCommand(async () =>
                {
                    isBusy = true;
                    await execute();
                    isBusy = false;
                }, (a) => !isBusy && (canExecute?.Invoke(a) ?? true),
                e =>
                {
                    isBusy = false;
                    onException?.Invoke(e);
                }, continueOnCapturedContext);
        }

        public bool CanExecute(object? parameter)
        {
            return command.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            ((ICommand)command).Execute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { command.CanExecuteChanged += value; }
            remove { command.CanExecuteChanged -= value; }
        }
    }
}