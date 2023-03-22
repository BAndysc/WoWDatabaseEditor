using System.Collections.Generic;

namespace WDE.Common.Collections;

public static class IndexedCollectionExtensions
{
    public static IIndexedCollection<T> AsIndexedCollection<T>(this IReadOnlyList<T> list)
    {
        return new IndexedCollectionReadOnlyListWrapper<T>(list);
    }
}