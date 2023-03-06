using System;

namespace WDE.Common.Collections;

/// <summary>
/// Collection that can be indexed by integer, but doesn't have explicit enumerator
/// as it is not meant to be iterated over (it can be costly)
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIndexedCollection<out T>
{
    static IIndexedCollection<T> Empty => EmptyIndexedCollection<T>.Instance;

    T this[int index] { get; }
    int Count { get; }
    
    event Action? OnCountChanged;
}