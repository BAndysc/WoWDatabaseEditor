using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WDE.Common.Utils;

namespace WDE.Common.Collections;

internal class SynchronizedIndexedCollectionWrapper<T> : IIndexedCollection<SynchronizedIndexedCollectionElementWrapper<T>>
{
    private readonly IAsyncIndexedCollection<T> asyncCollection;
    private bool hasCachedCount = false;
    private bool countIsBeingUpdated = false;
    private int cachedCount = 0;

    private Dictionary<int, SynchronizedIndexedCollectionElementWrapper<T>> items = new();

    public SynchronizedIndexedCollectionWrapper(IAsyncIndexedCollection<T> asyncCollection)
    {
        this.asyncCollection = asyncCollection;
    }

    private async Task UpdateCount()
    {
        Debug.Assert(!countIsBeingUpdated);
        countIsBeingUpdated = true;
        cachedCount = await asyncCollection.GetCount();
        hasCachedCount = true;
        countIsBeingUpdated = false;
        OnCountChanged?.Invoke();
    }
    
    public SynchronizedIndexedCollectionElementWrapper<T> this[int index]
    {
        get
        {
            if (items.TryGetValue(index, out var item))
                return item;
            
            var newItem = new SynchronizedIndexedCollectionElementWrapper<T>(asyncCollection.GetRow(index));
            items[index] = newItem;
            return newItem;
        }
    }

    public int Count
    {
        get
        {
            if (hasCachedCount)
                return cachedCount;

            if (countIsBeingUpdated)
                return 0;

            UpdateCount().ListenErrors();

            return 0;
        }
    }

    public event Action? OnCountChanged;
}