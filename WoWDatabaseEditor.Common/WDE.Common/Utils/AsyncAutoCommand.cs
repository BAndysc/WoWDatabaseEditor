using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using WDE.Common.Annotations;
using WDE.Common.Tasks;

namespace WDE.Common.Utils
{
    public class AsyncAutoCommand : ICommand
    {
        private bool isBusy;

        private readonly AsyncCommand command;

        public AsyncAutoCommand(Func<Task> execute,
            Func<object?, bool>? canExecute = null,
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
                    Console.WriteLine(e);
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
                GlobalApplication.MainThread.Dispatch(command.RaiseCanExecuteChanged);
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

        public void RaiseCanExecuteChanged()
        {
            command.RaiseCanExecuteChanged();
        }

        public event EventHandler? CanExecuteChanged
        {
            add => command.CanExecuteChanged += value;
            remove => command.CanExecuteChanged -= value;
        }
    }

    public class AsyncAutoCommand<T> : ICommand
    {
        private bool isBusy;

        private readonly AsyncCommand<T> command;

        public AsyncAutoCommand([NotNull]
            Func<T, Task> execute,
            Func<object?, bool>? canExecute = null,
            Action<Exception>? onException = null,
            bool continueOnCapturedContext = false)
        {
            command = new AsyncCommand<T>(async t =>
                {
                    IsBusy = true;
                    await execute(t);
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
                GlobalApplication.MainThread.Dispatch(command.RaiseCanExecuteChanged);
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