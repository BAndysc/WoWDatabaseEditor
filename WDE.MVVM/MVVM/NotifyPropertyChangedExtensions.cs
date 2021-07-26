using System;
using System.ComponentModel;
using System.Linq.Expressions;
using WDE.MVVM.Observable;

namespace WDE.MVVM
{
    public static class NotifyPropertyChangedExtensions
    {
        public static IObservable<T> ToObservable<T, R>(this R obj, Expression<Func<R, T>> getter) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToObservable<T, R>(obj, getter);
        }
        
        public static IObservable<T> ToObservable<T, R>(this R obj, Expression<Func<T>> getter) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToObservable<T, R>(obj, getter);
        }
        
        public static IObservable<Unit> ToObservable<R>(this R obj) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToUnitObservable<R>(obj);
        }
    }
}