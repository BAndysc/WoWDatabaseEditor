using System;

namespace WDE.MVVM.Observable.Functional;

internal class WhereObservable<T> : IObservable<T>
{
    private readonly IObservable<T> observable;
    private readonly Func<T, bool> condition;

    public WhereObservable(IObservable<T> observable, Func<T, bool> condition)
    {
        this.observable = observable;
        this.condition = condition;
    }
        
    public IDisposable Subscribe(IObserver<T> observer)
    {
        return observable.SubscribeAction(val =>
        {
            if (condition(val))
                observer.OnNext(val);
        });
    }
}