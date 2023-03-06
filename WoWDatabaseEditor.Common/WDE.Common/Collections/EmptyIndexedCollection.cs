using System;

namespace WDE.Common.Collections;

internal class EmptyIndexedCollection<T> : IIndexedCollection<T>
{
    public static readonly EmptyIndexedCollection<T> Instance = new();
    
    public T this[int index] => throw new IndexOutOfRangeException();
    public int Count => 0;
    public event Action? OnCountChanged;
}