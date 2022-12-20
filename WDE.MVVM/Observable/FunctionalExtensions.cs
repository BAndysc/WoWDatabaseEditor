using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using WDE.MVVM.Observable.Functional;
using WDE.MVVM.Utils;

namespace WDE.MVVM.Observable
{
    public static class FunctionalExtensions
    {
        public static IObservable<R> Select<T, R>(this IObservable<T> observable, Func<T, R> converter)
        {
            return new SelectObservable<T, R>(observable, converter);
        }

        public static IObservable<T> Where<T>(this IObservable<T> observable, Func<T, bool> converter)
        {
            return new WhereObservable<T>(observable, converter);
        }

        public static IObservable<bool> Not(this IObservable<bool> observable)
        {
            return observable.Select(t => !t);
        }

        public static IObservable<int> ToCountChangedObservable<T>(this ObservableCollection<T> collection)
        {
            return collection.ToObservable(o => o.Count);
        }

        public static IObservable<CollectionEvent<T>> ToStreamForChanges<T>(this ObservableCollection<T> collection)
        {
            return new ObservableCollectionStream<T>(collection, false, false);
        }

        public static IObservable<CollectionEvent<T>> ToStream<T>(this ObservableCollection<T> collection, bool onRemoveOnDispose)
        {
            return new ObservableCollectionStream<T>(collection, onRemoveOnDispose, true);
        }

        public static System.IDisposable DisposeOnRemove<T>(this ObservableCollection<T> collection, Func<bool> shouldDispose) where T : System.IDisposable
        {
            return collection.ToStream(true).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Remove && shouldDispose())
                    e.Item.Dispose();
            });
        }
        
        public static System.IDisposable DisposeOnRemove<T>(this ObservableCollection<T> collection) where T : System.IDisposable
        {
            return collection.ToStream(true).SubscribeAction(e =>
            {
                if (e.Type == CollectionEventType.Remove)
                    e.Item.Dispose();
            });
        }

        public static System.IDisposable ToStreamForEach<T, R>(this ObservableCollection<T> source, Expression<Func<T, R>> get, Action<T, R> onNext) where T : class, INotifyPropertyChanged
        {
            Dictionary<T, System.IDisposable> disposables = new(ReferenceEqualityComparer<T>.Instance);
            return source.ToStream(true).SubscribeAction(i =>
            {
                if (i.Type == CollectionEventType.Add)
                {
                    disposables.Add(i.Item, i.Item.ToObservable(get).SubscribeAction(e =>
                    {
                        onNext(i.Item, e);
                    }));
                }
                else if (i.Type == CollectionEventType.Remove)
                {
                    if (disposables.TryGetValue(i.Item, out var disp))
                    {
                        disp.Dispose();
                        disposables.Remove(i.Item);
                    }
                }
            });
        }
        
        public static System.IDisposable ToStreamForEachValue<T>(this ObservableCollection<T> source, Action<T, PropertyValueChangedEventArgs> onNext) where T : class, INotifyPropertyValueChanged
        {
            Dictionary<T, System.IDisposable> disposables = new(ReferenceEqualityComparer<T>.Instance);
            return source.ToStream(true).SubscribeAction(i =>
            {
                if (i.Type == CollectionEventType.Add)
                {
                    disposables.Add(i.Item, i.Item.ToObservableValue().SubscribeAction(e =>
                    {
                        onNext(i.Item, e);
                    }));
                }
                else if (i.Type == CollectionEventType.Remove)
                {
                    if (disposables.TryGetValue(i.Item, out var disp))
                    {
                        disp.Dispose();
                        disposables.Remove(i.Item);
                    }
                }
            });
        }

        public static IObservable<CollectionEvent<T>> ToStream<T>(this INotifyCollectionChanged collection, IEnumerable<T> initial)
        {
            return new ObservableCollectionStream<T>(initial, collection, false, true);
        }
    }
}