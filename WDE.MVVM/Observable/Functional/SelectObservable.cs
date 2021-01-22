using System;

namespace WDE.MVVM.Observable.Functional
{
    internal class SelectObservable<T, R> : IObservable<R>
    {
        private readonly IObservable<T> observable;
        private readonly Func<T, R> converter;

        public SelectObservable(IObservable<T> observable, Func<T, R> converter)
        {
            this.observable = observable;
            this.converter = converter;
        }
        
        public IDisposable Subscribe(IObserver<R> observer)
        {
            return observable.Subscribe(val => observer.OnNext(converter(val)));
        }
    }
}