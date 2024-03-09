using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using WDE.Common;

namespace WDE.MVVM.Observable.Functional
{
    public class ObservableCollectionStream<T> : IObservable<CollectionEvent<T>>
    {
        private readonly IEnumerable<T> initial;
        private readonly INotifyCollectionChanged notifyCollectionChanged;
        private readonly bool onRemoveOnDispose;
        private readonly bool includeInitial;

        public ObservableCollectionStream(IEnumerable<T> initial, INotifyCollectionChanged notifyCollectionChanged, bool onRemoveOnDispose, bool includeInitial)
        {
            this.initial = initial;
            this.notifyCollectionChanged = notifyCollectionChanged;
            this.onRemoveOnDispose = onRemoveOnDispose;
            this.includeInitial = includeInitial;
        }

        public ObservableCollectionStream(ObservableCollection<T> collection, bool onRemoveOnDispose, bool includeInitial) : this(collection, collection, onRemoveOnDispose, includeInitial)
        {
        }

        public IDisposable Subscribe(IObserver<CollectionEvent<T>> observer)
        {
            return new Subscription(initial, notifyCollectionChanged, observer, onRemoveOnDispose, includeInitial);
        }

        private class Subscription : IDisposable
        {
            private readonly IEnumerable<T> initial;
            private readonly INotifyCollectionChanged notifyCollectionChanged;
            private readonly IObserver<CollectionEvent<T>> observer;
            private readonly bool removeOnDispose;

            public Subscription(IEnumerable<T> initial, INotifyCollectionChanged changed,
                IObserver<CollectionEvent<T>> observer, bool removeOnDispose, bool includeInitial)
            {
                this.initial = initial;
                notifyCollectionChanged = changed;
                
                this.observer = observer;
                this.removeOnDispose = removeOnDispose;
                changed.CollectionChanged += CollectionOnCollectionChanged;

                if (includeInitial)
                {
                    int index = 0;
                    foreach (var t in initial)
                    {
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Add, t, index++));   
                    }   
                }
            }

            public void Dispose()
            {
                notifyCollectionChanged.CollectionChanged -= CollectionOnCollectionChanged;
                if (removeOnDispose)
                {
                    foreach (var t in initial)
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Remove, t, 0));
                }
            }
            
            private void CollectionOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                int index = 0;
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        foreach (T item in e.NewItems!)
                        {
                            observer.OnNext(new CollectionEvent<T>(CollectionEventType.Add, item, e.NewStartingIndex + index));
                            index++;
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        foreach (T item in e.OldItems!)
                        {
                            observer.OnNext(new CollectionEvent<T>(CollectionEventType.Remove, item, e.OldStartingIndex + index));
                            index++;
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        if (e.OldItems!.Count != 1 || e.NewItems!.Count != 1)
                            throw new ArgumentOutOfRangeException();
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Remove, (T)e.OldItems![0]!, e.OldStartingIndex));
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Add, (T)e.NewItems![0]!, e.OldStartingIndex));
                        
                        break;
                    case NotifyCollectionChangedAction.Move:
                        if (e.OldItems!.Count != 1 || e.NewItems!.Count != 1)
                            throw new ArgumentOutOfRangeException();
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Remove, (T)e.OldItems![0]!, e.OldStartingIndex));
                        observer.OnNext(new CollectionEvent<T>(CollectionEventType.Add, (T)e.NewItems![0]!, e.NewStartingIndex));
                        
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        LOG.LogCritical("ObservableCollection reset event is not supported in ToStream(), because it is not possible to do so.");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}