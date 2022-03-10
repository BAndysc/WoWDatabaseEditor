using System;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace WDE.Common.Avalonia;

public static class Extensions
{
    public static T? SelfOrVisualAncestor<T>(this IVisual visual) where T : class
    {
        if (visual is T t)
            return t;
        return visual.FindAncestorOfType<T>();
    }

    // public static IObservable<T> Throttle<T>(this System.IObservable<T> observable, TimeSpan timeSpan)
    // {
    //     return new ThrottlingObservable<T>(observable, timeSpan);
    // }
}

public class ThrottlingObservable<T> : IObservable<T>
{
    private readonly IObservable<T> observable;
    private readonly TimeSpan timeSpan;

    public ThrottlingObservable(System.IObservable<T> observable, TimeSpan timeSpan)
    {
        this.observable = observable;
        this.timeSpan = timeSpan;
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        return new ThrottleSub(observable, observer, timeSpan);
    }
    
    internal class ThrottleSub : System.IDisposable
    {
        private System.IDisposable? delay;
        private System.IDisposable? sub;
        public ThrottleSub(IObservable<T> observable, IObserver<T> observer, TimeSpan timeSpan)
        {
            sub = observable.Subscribe(t =>
            {
                delay?.Dispose();
                delay = DispatcherTimer.RunOnce(() =>
                {
                    observer.OnNext(t);
                    delay = null;
                }, timeSpan);
            }, observer.OnError, observer.OnCompleted);
        }

        public void Dispose()
        {
            delay?.Dispose();
            sub?.Dispose();
            delay = null;
            sub = null;
        }
    }
}
