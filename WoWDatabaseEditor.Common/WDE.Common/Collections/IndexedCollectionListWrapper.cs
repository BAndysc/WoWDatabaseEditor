using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace WDE.Common.Collections;

internal class IndexedCollectionReadOnlyListWrapper<T> : IIndexedCollection<T>
{
    private readonly IReadOnlyList<T> list;

    public IndexedCollectionReadOnlyListWrapper(IReadOnlyList<T> list)
    {
        this.list = list;
        if (list is INotifyCollectionChanged incc)
        {
            incc.CollectionChanged += (_, _) => OnCountChanged?.Invoke();
        }
    }

    public T this[int index] => list[index];
    public int Count => list.Count;
    public event Action? OnCountChanged;
}
