﻿using System;
using System.Threading.Tasks;
using System.Windows.Input;
using AsyncAwaitBestPractices.MVVM;
using Prism.Commands;
using WDE.Common.Annotations;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;

namespace WDE.Common.Utils
{
    public class AsyncAutoCommand : ICommand, IAsyncCommand
    {
        private bool isBusy;

        private readonly AsyncCommand command;
        
        public AsyncAutoCommand(Func<Task> execute,
            Func<bool>? canExecute = null,
            Action<Exception>? onException = null,
            bool continueOnCapturedContext = false)
        {
            command = new AsyncCommand(async () =>
                {
                    IsBusy = true;
                    try
                    {
                        await execute();
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                },
                _ => !isBusy && (canExecute?.Invoke() ?? true),
                e =>
                {
                    IsBusy = false;
                    if (e is not TaskCanceledException)
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

        public Task ExecuteAsync()
        {
            return command.ExecuteAsync();
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
    
    public class AsyncAutoCommand<T> : ICommand, IAsyncCommand<T>
    {
        private bool isBusy;

        private readonly AsyncCommand<T> command;

        public AsyncAutoCommand([NotNull]
            Func<T, Task> execute,
            Func<T?, bool>? canExecute = null,
            Action<Exception>? onException = null,
            bool continueOnCapturedContext = false)
        {
            command = new AsyncCommand<T>(async t =>
                {
                    IsBusy = true;
                    try
                    {
                        await execute(t);
                    }
                    finally
                    {
                        IsBusy = false;
                    }
                },
                a => !isBusy && (canExecute?.Invoke((T?)a) ?? true),
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

        public Task ExecuteAsync(T parameter)
        {
            return command.ExecuteAsync(parameter);
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
    
    
    internal class AsyncCommandExceptionWrap<T> : ICommand, IAsyncCommand where T : Exception
    {
        private readonly IAsyncCommand parent;
        private readonly Action<T>? onError;
        private readonly Func<T, Task>? onErrorTask;

        public AsyncCommandExceptionWrap(IAsyncCommand parent, Action<T> onError)
        {
            this.parent = parent;
            this.onError = onError;
        }
        
        public AsyncCommandExceptionWrap(IAsyncCommand parent, Func<T, Task> onError)
        {
            this.parent = parent;
            this.onErrorTask = onError;
        }
        
        public bool CanExecute(object? parameter)
        {
            return parent.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            ExecuteAsync().ListenErrors();
        }

        public async Task ExecuteAsync()
        {
            try
            {
                await parent.ExecuteAsync();
            }
            catch (T e)
            {
                onError?.Invoke(e);
                if (onErrorTask != null)
                    await onErrorTask(e);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            parent.RaiseCanExecuteChanged();
        }

        public event EventHandler? CanExecuteChanged
        {
            add => parent.CanExecuteChanged += value;
            remove => parent.CanExecuteChanged -= value;
        }
    }
    
    internal class AsyncCommandExceptionWrap<T, R> : ICommand, IAsyncCommand<R> where T : Exception
    {
        private readonly IAsyncCommand<R> parent;
        private readonly Action<T>? onError;
        private readonly Func<T, Task>? onErrorTask;

        public AsyncCommandExceptionWrap(IAsyncCommand<R> parent, Action<T> onError)
        {
            this.parent = parent;
            this.onError = onError;
        }
        
        public AsyncCommandExceptionWrap(IAsyncCommand<R> parent, Func<T, Task> onError)
        {
            this.parent = parent;
            this.onErrorTask = onError;
        }
        
        public bool CanExecute(object? parameter)
        {
            return parent.CanExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            ExecuteAsync((R)parameter!).ListenErrors();
        }

        public async Task ExecuteAsync(R parameter)
        {
            try
            {
                await parent.ExecuteAsync(parameter);
            }
            catch (T e)
            {
                onError?.Invoke(e);
                if (onErrorTask != null)
                    await onErrorTask(e);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            parent.RaiseCanExecuteChanged();
        }

        public event EventHandler? CanExecuteChanged
        {
            add => parent.CanExecuteChanged += value;
            remove => parent.CanExecuteChanged -= value;
        }
    }
    
    public static class CommandExtensions
    {
        public static IAsyncCommand WrapException<T>(this IAsyncCommand cmd, Action<T> onError) where T : Exception
        {
            return new AsyncCommandExceptionWrap<T>(cmd, onError);
        }
        
        public static IAsyncCommand WrapMessageBox<T>(this IAsyncCommand cmd, IMessageBoxService messageBoxService, string? header = null) where T : Exception
        {
            return new AsyncCommandExceptionWrap<T>(cmd, async (e) =>
            {
                Console.WriteLine(e);
                await messageBoxService.SimpleDialog("Error", header ?? "Error occured while executing the command", e.Message);
            });
        }
        
        public static IAsyncCommand<R> WrapMessageBox<T, R>(this IAsyncCommand<R> cmd, IMessageBoxService messageBoxService, string? header = null) where T : Exception
        {
            return new AsyncCommandExceptionWrap<T, R>(cmd, async (e) =>
            {
                Console.WriteLine(e);
                await messageBoxService.SimpleDialog("Error", header ?? "Error occured while executing the command", e.Message);
            });
        }
    }
}