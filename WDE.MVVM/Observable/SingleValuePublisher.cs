using System;

namespace WDE.MVVM.Observable
{
    public class SingleValuePublisher<T> : IObservable<T>
    {
        private readonly T value;

        public SingleValuePublisher(T value)
        {
            this.value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(value);
            observer.OnCompleted();
            return new EmptyDisposable();
        }
    }
}