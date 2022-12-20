using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;
using WDE.MVVM.Observable;

namespace WDE.MVVM;

internal class NotifyCommandCanExecuteObservable : IObservable<Unit>
{
    private readonly ICommand command;
    
    public NotifyCommandCanExecuteObservable(ICommand command)
    {
        this.command = command;
    }

    public IDisposable Subscribe(IObserver<Unit> observer)
    {
        return new Subscription(observer, command);
    }

    class Subscription : IDisposable
    {
        private readonly IObserver<Unit> observer;
        private readonly ICommand command;

        public Subscription(IObserver<Unit> observer, ICommand command)
        {
            this.observer = observer;
            this.command = command;
            command.CanExecuteChanged += CommandOnCanExecuteChanged;
            observer.OnNext(default);
        }

        private void CommandOnCanExecuteChanged(object? sender, EventArgs e)
        {
            observer.OnNext(default);
        }

        public void Dispose()
        {
            command.CanExecuteChanged -= CommandOnCanExecuteChanged;
        }
    }
}