using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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

        public static IObservable<CollectionEvent<T>> ToStream<T>(this ObservableCollection<T> collection)
        {
            return new ObservableCollectionStream<T>(collection);
        }

        public static IObservable<CollectionEvent<T>> ToStream<T>(this INotifyCollectionChanged collection, IEnumerable<T> initial)
        {
            return new ObservableCollectionStream<T>(initial, collection);
        }

        public static void RemoveAll<T>(this ObservableCollection<T> collection)
        {
            for (int i = collection.Count - 1; i >= 0; --i)
                collection.RemoveAt(i);
        }
    }
}