using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace WDE.MVVM.Observable.Functional
{
    public class ObservableCollectionStream<T> : IObservable<CollectionEvent<T>>
    {
        private readonly ObservableCollection<T> collection;

        public ObservableCollectionStream(ObservableCollection<T> collection)
        {
            this.collection = collection;
        }

        public IDisposable Subscribe(IObserver<CollectionEvent<T>> observer)
        {
            return new Subscription(collection, observer);
        }

        private class Subscription : IDisposable
        {
            private readonly ObservableCollection<T> collection;
            private readonly IObserver<CollectionEvent<T>> observer;

            public Subscription(ObservableCollection<T> collection, IObserver<CollectionEvent<T>> observer)
            {
                this.collection = collection;
                this.observer = observer;
                collection.CollectionChanged += CollectionOnCollectionChanged;

                for (var index = 0; index < collection.Count; index++)
                    observer.OnNext(new CollectionEvent<T>(CollectionEventType.Add, collection[index], index));
            }

            public void Dispose()
            {
                collection.CollectionChanged -= CollectionOnCollectionChanged;
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
                        
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}