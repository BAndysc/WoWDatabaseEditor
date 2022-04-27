using System;
using System.ComponentModel;
using System.Linq.Expressions;
using WDE.MVVM.Observable.Functional;

namespace WDE.MVVM.Observable
{
    public static class ObservableExtensions
    {
        /**
         * wraps ob
         */
        public static IObservable<T> Safe<T>(this IObservable<T> observable)
        {
            return new SafeObservable<T>(observable);
        }

        public static IDisposable SubscribeAction<T>(this IObservable<T> observable, Action<T> onNext)
        {
            return observable.Subscribe(new DelegateObserver<T>(onNext));
        }
        
        public static IDisposable SubscribeOnce<T>(this IObservable<T> observable, Action<T> onNext)
        {
            System.IDisposable? disposable = null;
            bool fired = false;
            disposable = observable.Subscribe(new DelegateObserver<T>(x =>
            {
                onNext(x);
                fired = true;
                disposable?.Dispose();
            }));
            if (fired)
            {
                disposable.Dispose();
                disposable = new EmptyDisposable();
            }
            return disposable;
        }
        
        public static IDisposable Subscribe<T, R>(this R observable, Expression<Func<R, T>> propertyGetter, Action<T> onNext) where R : INotifyPropertyChanged
        {
            return observable.ToObservable(propertyGetter).Subscribe(new DelegateObserver<T>(onNext));
        }
    }
}