using System;
using WDE.Common;

namespace WDE.MVVM.Observable.Functional;

public class SafeObservable<T> : IObservable<T>
{
    private readonly IObservable<T> source;

    public SafeObservable(IObservable<T> source)
    {
        this.source = source;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return source.Subscribe(new SafeObserver(observer));
    }

    public class SafeObserver : IObserver<T>
    {
        private readonly IObserver<T> observer;

        public SafeObserver(IObserver<T> observer)
        {
            this.observer = observer;
        }

        public void OnCompleted()
        {
            try
            {
                observer.OnCompleted();
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }

        public void OnError(Exception error)
        {
            try
            {
                observer.OnError(error);
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }

        public void OnNext(T value)
        {
            try
            {
                observer.OnNext(value);
            }
            catch (Exception e)
            {
                LOG.LogError(e);
            }
        }
    }
}