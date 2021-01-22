using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WDE.MVVM
{
    public static class NotifyPropertyChangedExtensions
    {
        public static IObservable<T> ToObservable<T, R>(this R obj, Expression<Func<R, T>> getter) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToObservable<T, R>(obj, getter);
        }
    }
}