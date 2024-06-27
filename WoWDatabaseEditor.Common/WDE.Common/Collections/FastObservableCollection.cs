using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using DynamicData.Binding;

namespace WDE.Common.Collections;

// ObservableCollection with pausing notifications, but faster than ObservableCollection<T> because less allocs when paused
public class FastObservableCollection<T> : ObservableCollection<T>
{
    private int pausedNotifications;

    protected override void InsertItem(int index, T item)
    {
        if (pausedNotifications > 0)
        {
            CheckReentrancy();
            Items.Insert(index, item);
            return;
        }
        base.InsertItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        if (pausedNotifications > 0)
        {
            CheckReentrancy();
            Items.RemoveAt(index);
            return;
        }
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        if (pausedNotifications > 0)
        {
            CheckReentrancy();
            Items.Clear();
            return;
        }
        base.ClearItems();
    }

    public SuspendedNotifications SuspendNotifications()
    {
        pausedNotifications++;
        return new SuspendedNotifications(this);
    }

    private void InvokeResetNotification()
    {
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public struct SuspendedNotifications(FastObservableCollection<T> collection) : IDisposable
    {
        private readonly FastObservableCollection<T> collection = collection;

        public void Dispose()
        {
            collection.pausedNotifications--;
            if (collection.pausedNotifications == 0)
                collection.InvokeResetNotification();
        }
    }
}