using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData.Binding;

namespace WDE.Common.Utils;

// note: it will not work 100% correctly if the items are not unique
public class AsyncFilteredObservableList<T> : INotifyCollectionChanged, IReadOnlyList<T>, IList<T>, IList
{
    private readonly Func<string, CancellationToken, IReadOnlyList<T>, List<T>?> filterer;
    private readonly IComparer<T>? comparer;
    private IReadOnlyList<T> elements;
    private List<T> addedElements = new();

    private ObservableCollectionExtended<T> collection;

    private CancellationTokenSource? currentToken;

    private List<T> itemsToRemove = new();
    private List<T> itemsToAdd = new();

    private bool isFiltering => currentToken != null;

    private string? currentFilter = null;
    
    public AsyncFilteredObservableList(IReadOnlyList<T> sourceElements,
        Func<string, CancellationToken, IReadOnlyList<T>, List<T>?> filterer,
        IComparer<T>? comparer)
    {
        this.elements = sourceElements;
        this.filterer = filterer;
        this.comparer = comparer;
        collection = new ObservableCollectionExtended<T>();
        collection.AddRange(elements);
    }

    public void SetFilter(string filter)
    {
        if (currentToken != null)
            currentToken.Cancel();
        currentToken = new CancellationTokenSource();
        DoFilter(filter, currentToken).ListenErrors();
    }

    private void Refilter()
    {
        SetFilter(currentFilter ?? "");
    }

    private async Task DoFilter(string filter, CancellationTokenSource token)
    {
        currentFilter = filter;
        if (string.IsNullOrEmpty(filter))
        {
            using var _ = collection.SuspendNotifications();
            collection.Clear();
            collection.AddRange(elements);
        }
        else
        {
            var filtered = await Task.Run(() =>
            {
                var list = filterer(filter, token.Token, elements.Concat(addedElements));
                if (token.IsCancellationRequested)
                    return null;
            
                if (comparer != null)
                    list!.Sort(comparer);

                return list;
            });
        
            if (token.IsCancellationRequested)
                return;

            if (currentToken != token)
                return;
            
            using var _ = collection.SuspendNotifications();
            collection.Clear();
            collection.AddRange(filtered!);
        }

        currentToken = null;
        
        if (itemsToAdd.Count > 0 || itemsToRemove.Count > 0)
        {
            foreach (var item in itemsToRemove)
                addedElements.Remove(item);
            foreach (var item in itemsToAdd)
                addedElements.Add(item);
            itemsToAdd.Clear();
            itemsToRemove.Clear();
            Refilter();
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => collection.CollectionChanged += value;
        remove => collection.CollectionChanged -= value;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)collection).GetEnumerator();
    }

    public bool Contains(T item) => collection.Contains(item);

    public int Count => collection.Count;

    public IReadOnlyList<T> SourceItems => elements.Concat(addedElements);
    
    public void Add(T item)
    {
        if (isFiltering)
        {
            if (itemsToRemove.Contains(item))
                itemsToRemove.Remove(item);
            else
                itemsToAdd.Add(item);
        }
        else
        {
            addedElements.Add(item);
        }
    }

    public bool RemoveAddedElement(T item)
    {
        if (isFiltering)
        {
            if (itemsToAdd.Contains(item))
                return itemsToAdd.Remove(item);

            if (SourceItems.Contains(item))
            {
                itemsToRemove.Add(item);
                return true;
            }

            return false;
        }
        else
        {
            return addedElements.Remove(item);
        }
    }

    
    public bool Remove(T item) => throw new NotSupportedException();
    
    public int Add(object? value) => throw new NotSupportedException();

    void IList.Clear() => throw new NotSupportedException();

    public bool Contains(object? value) => ((IList)collection).Contains(value);

    public int IndexOf(object? value) => ((IList)collection).IndexOf(value);

    public void Insert(int index, object? value) => throw new NotSupportedException();

    public void Remove(object? value) => throw new NotSupportedException();

    void IList.RemoveAt(int index) => throw new NotSupportedException();

    public bool IsFixedSize => false;

    bool IList.IsReadOnly => true;

    object? IList.this[int index]
    {
        get => collection[index];
        set => throw new NotSupportedException();
    }
    
    void ICollection<T>.Clear() => throw new NotSupportedException();

    public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();

    public void CopyTo(Array array, int index) => throw new NotImplementedException();

    public bool IsSynchronized => false;
    
    public object SyncRoot => null!;

    bool ICollection<T>.IsReadOnly => true;

    public int IndexOf(T item) => collection.IndexOf(item);

    public void Insert(int index, T item) => throw new NotSupportedException();

    void IList<T>.RemoveAt(int index) => throw new NotSupportedException();

    public T this[int index]
    {
        get => collection[index];
        set => collection[index] = value;
    }
}
