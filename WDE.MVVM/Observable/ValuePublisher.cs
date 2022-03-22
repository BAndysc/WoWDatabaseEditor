using System;
using System.Collections.Generic;

namespace WDE.MVVM.Observable;

public class ValuePublisher<T> : IObservable<T>
{
    private bool hasValue;
    private T? value;
    public void Publish(T value)
    {
        hasValue = true;
        this.value = value;
            
        foreach (var s in subs)
            s.Fire(value);
    }
        
    public IDisposable Subscribe(IObserver<T> observer)
    {
        return new Subscription(this, observer);
    }

    public class Subscription : System.IDisposable
    {
        private readonly ValuePublisher<T> publisher;
        private readonly IObserver<T> observer;

        public Subscription(ValuePublisher<T> publisher, IObserver<T> observer)
        {
            this.publisher = publisher;
            this.observer = observer;
            publisher.Add(this);
            if (publisher.hasValue)
                observer.OnNext(publisher.value!);
        }

        public void Dispose()
        {
            publisher.Remove(this);
        }

        public void Fire(T value)
        {
            observer.OnNext(value);
        }
    }

    private List<Subscription> subs = new();
        
    private void Remove(Subscription subscription)
    {
        subs.Remove(subscription);
    }

    private void Add(Subscription subscription)
    {
        subs.Add(subscription);
    }
}