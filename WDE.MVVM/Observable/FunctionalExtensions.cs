using System;
using WDE.MVVM.Observable.Functional;

namespace WDE.MVVM.Observable
{
    public static class FunctionalExtensions
    {
        public static IObservable<R> Select<T, R>(this IObservable<T> observable, Func<T, R> converter)
        {
            return new SelectObservable<T, R>(observable, converter);
        }
        
        public static IObservable<bool> Not(this IObservable<bool> observable)
        {
            return observable.Select(t => !t);
        }
    }
}