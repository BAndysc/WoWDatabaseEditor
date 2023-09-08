using System;
using System.Collections;
using System.Collections.Generic;

namespace WDE.Common.Utils;

/// <summary>
/// efficient list for small lists
/// </summary>
/// <typeparam name="T"></typeparam>
public struct SmallReadOnlyList<T> : IReadOnlyList<T>
{
    private T item0;
    private T item1;
    private T item2;
    private List<T> more;
    private int count;
    
    public SmallReadOnlyList(T item0)
    {
        this.item0 = item0;
        item1 = default!;
        item2 = default!;
        more = null!;
        count = 1;
    }
    
    public SmallReadOnlyList(T item0, T item1)
    {
        this.item0 = item0;
        this.item1 = item1;
        item2 = default!;
        more = null!;
        count = 2;
    }
    
    public SmallReadOnlyList(T item0, T item1, T item2)
    {
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        more = null!;
        count = 3;
    }

    public SmallReadOnlyList(IEnumerable<T> source)
    {
        item0 = default!;
        item1 = default!;
        item2 = default!;
        more = null!;
        count = 0;
        foreach (var x in source)
        {
            if (count == 0)
                item0 = x;
            else if (count == 1)
                item1 = x;
            else if (count == 2)
                item2 = x;
            else
            {
                more ??= new List<T>();
                more.Add(x);
            }

            count++;
        }
    }
    
    
    public IEnumerator<T> GetEnumerator()
    {
        if (count >= 1)
            yield return item0;

        if (count >= 2)
            yield return item1;
        
        if (count >= 3)
            yield return item2;

        if (count >= 4)
        {
            for (int i = 3; i < count; ++i)
                yield return more[i - 3];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var x in this)
            yield return x;
    }

    public int Count => count;

    public T this[int index]
    {
        get
        {
            if (index < 0 || index > count)
                throw new IndexOutOfRangeException();

            if (index == 0)
                return item0;
            
            if (index == 1)
                return item1;

            if (index == 2)
                return item2;
            
            return more[index - 3];
        }
    }
}