using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows.Input;
using WDE.MVVM.Observable;

namespace WDE.MVVM
{
    public static class NotifyPropertyChangedExtensions
    {
        public static IObservable<PropertyValueChangedEventArgs> ToObservableValue<T>(this T obj) where T : INotifyPropertyValueChanged
        {
            return new NotifyPropertyValueToObservable<T>(obj);
        }
        
        public static IObservable<T> ToObservable<T, R>(this R obj, Expression<Func<R, T>> getter) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToObservable<T, R>(obj, getter);
        }
        
        public static IObservable<Unit> ToObservable(this ICommand obj)
        {
            return new NotifyCommandCanExecuteObservable(obj);
        }

        public static IObservable<T> ToObservable<T, R>(this R obj, Expression<Func<T>> getter) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToObservable<T, R>(obj, getter);
        }
        
        public static IObservable<Unit> ToObservable<R>(this R obj, string propertyName) where R : INotifyPropertyChanged
        {
            return new NotifySpecificPropertyToUnitObservable<R>(obj, propertyName);
        }
        
        public static IObservable<Unit> ToObservable<R>(this R obj) where R : INotifyPropertyChanged
        {
            return new NotifyPropertyToUnitObservable<R>(obj);
        }
    }
}