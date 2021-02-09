using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WDE.MVVM.Observable
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeAction<T>(this IObservable<T> observable, Action<T> onNext)
        {
            return observable.Subscribe(new DelegateObserver<T>(onNext));
        }
        
        public static IDisposable Subscribe<T, R>(this R observable, Expression<Func<R, T>> propertyGetter, Action<T> onNext) where R : INotifyPropertyChanged
        {
            return observable.ToObservable(propertyGetter).Subscribe(new DelegateObserver<T>(onNext));
        }
    }
}