using System;
using System.Collections.Generic;

namespace WDE.MVVM.Observable
{
    public class OnDemandSingleValuePublisher<T> : IObservable<T>
    {
        private bool isFinished;
        public void Publish(T value)
        {
            isFinished = true;
            
            foreach (var s in subs)
                s.FireAndComplete(value);
            subs.Clear();
        }
        
        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (isFinished)
                return new EmptyDisposable();
            
            return new Subscription(this, observer);
        }

        public class Subscription : System.IDisposable
        {
            private readonly OnDemandSingleValuePublisher<T> publisher;
            private readonly IObserver<T> observer;

            public Subscription(OnDemandSingleValuePublisher<T> publisher, IObserver<T> observer)
            {
                this.publisher = publisher;
                this.observer = observer;
                publisher.Add(this);
            }

            public void Dispose()
            {
                publisher.Remove(this);
            }

            public void FireAndComplete(T value)
            {
                observer.OnNext(value);
                observer.OnCompleted();
            }
        }

        private List<Subscription> subs = new();
        
        private void Remove(Subscription subscription)
        {
            if (isFinished)
                return;
            
            subs.Remove(subscription);
        }

        private void Add(Subscription subscription)
        {
            if (isFinished)
                return;

            subs.Add(subscription);
        }
    }
}