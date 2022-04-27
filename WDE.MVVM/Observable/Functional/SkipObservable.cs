using System;

namespace WDE.MVVM.Observable.Functional;

internal class SkipObservable<T> : IObservable<T>
{
    private readonly IObservable<T> observable;
    private readonly int count;

    public SkipObservable(IObservable<T> observable, int count)
    {
        this.observable = observable;
        this.count = count;
    }
        
    public IDisposable Subscribe(IObserver<T> observer)
    {
        int index = 0;
        int count = this.count;
        return observable.SubscribeAction(val =>
        {
            if (index++ < count)
                return;
            observer.OnNext(val);
        });
    }
}