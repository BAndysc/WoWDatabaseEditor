using System;

namespace WDE.MVVM.Observable;

public interface IReadOnlyReactiveProperty<out T> : IObservable<T>
{
    public T Value { get; }
}