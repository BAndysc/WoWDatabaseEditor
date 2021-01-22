using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("WDE.MVVM.Test")]
namespace WDE.MVVM.Observable
{
    public class ReactiveProperty<T> : IObservable<T>, System.IDisposable
    {
        private bool isFinished;
        private List<Subscription> subscriptions = new();
        private T value;
        public T Value
        {
            get => value;
            set
            {
                if (Comparer<T>.Default.Compare(value, Value) == 0)
                    return;
                
                this.value = value;
                for (var index = subscriptions.Count - 1; index >= 0; index--)
                {
                    var sub = subscriptions[index];
                    sub.Publish();
                }
            }
        }

        public ReactiveProperty(T value)
        {
            this.value = value;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (isFinished)
                return new DummySubscription();
            
            var sub = new Subscription(this, observer);
            subscriptions.Add(sub);
            return sub;
        }

        public void Dispose()
        {
            for (var index = subscriptions.Count - 1; index >= 0; index--)
            {
                var sub = subscriptions[index];
                sub.Dispose();
            }
            isFinished = true;
        }

        internal int SubscriptionsCount => subscriptions.Count;

        class Subscription : System.IDisposable
        {
            private readonly ReactiveProperty<T> property;
            private readonly IObserver<T> observer;

            public Subscription(ReactiveProperty<T> property, IObserver<T> observer)
            {
                this.property = property;
                this.observer = observer;
                observer.OnNext(property.Value);
            }

            public void Dispose()
            {
                property.subscriptions.Remove(this);
            }

            public void Publish()
            {
                observer.OnNext(property.Value);
            }
        }

        struct DummySubscription : System.IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}