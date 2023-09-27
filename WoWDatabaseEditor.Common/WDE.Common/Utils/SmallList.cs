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

/// <summary>
/// efficient read-write list for small lists
/// </summary>
/// <typeparam name="T"></typeparam>
public struct SmallList<T> : IList<T>, IReadOnlyList<T>
{
    private T item0;
    private T item1;
    private T item2;
    private List<T> more;
    private int count;
    
    public SmallList(T item0)
    {
        this.item0 = item0;
        item1 = default!;
        item2 = default!;
        more = null!;
        count = 1;
    }
    
    public SmallList(T item0, T item1)
    {
        this.item0 = item0;
        this.item1 = item1;
        item2 = default!;
        more = null!;
        count = 2;
    }
    
    public SmallList(T item0, T item1, T item2)
    {
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        more = null!;
        count = 3;
    }
    
    public SmallList(T item0, T item1, T item2, T item3)
    {
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        more = new List<T>{ item3 };
        count = 4;
    }
    
    public SmallList(T item0, T item1, T item2, T item3, T item4)
    {
        this.item0 = item0;
        this.item1 = item1;
        this.item2 = item2;
        more = new List<T>{ item3, item4 };
        count = 5;
    }

    public SmallList(IEnumerable<T> source)
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

    public void Clear()
    {
        more?.Clear();
        count = 0;
    }

    public bool Contains(T item)
    {
        if (count >= 1 && EqualityComparer<T>.Default.Equals(item0, item))
            return true;

        if (count >= 2 && EqualityComparer<T>.Default.Equals(item1, item))
            return true;

        if (count >= 3 && EqualityComparer<T>.Default.Equals(item2, item))
            return true;

        if (count >= 4)
            return more.Contains(item);

        return false;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (count >= 1)
            array[arrayIndex] = item0;

        if (count >= 2)
            array[arrayIndex + 1] = item1;

        if (count >= 3)
            array[arrayIndex + 2] = item2;

        if (count >= 4)
            more.CopyTo(array, arrayIndex + 3);
    }

    public int Count => count;

    public bool IsReadOnly => false;

    public int IndexOf(T item)
    {
        if (count >= 1 && EqualityComparer<T>.Default.Equals(item0, item))
            return 0;

        if (count >= 2 && EqualityComparer<T>.Default.Equals(item1, item))
            return 1;

        if (count >= 3 && EqualityComparer<T>.Default.Equals(item2, item))
            return 2;

        if (count >= 4)
        {
            var index = more.IndexOf(item);
            if (index == -1)
                return -1;
            return index + 3;
        }

        return -1;
    }

    public void Add(T item)
    {
        if (count == 0)
        {
            item0 = item;
            count = 1;
        }
        else if (count == 1)
        {
            item1 = item;
            count = 2;
        }
        else if (count == 2)
        {
            item2 = item;
            count = 3;
        }
        else
        {
            more ??= new List<T>();
            more.Add(item);
            count++;
        }
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index > count)
            throw new IndexOutOfRangeException();

        if (index <= 2)
        {
            if (count >= 3)
            {
                more ??= new List<T>();
                more.Insert(0, item2);
            }

            item2 = item1;
            if (index <= 1)
                item1 = item0;

            if (index == 0)
                item0 = item;
            else if (index == 1)
                item1 = item;
            else if (index == 2)
                item2 = item;
            
            count++;
        }
        else
        {
            more ??= new List<T>();
            more.Insert(index - 3, item);
            count++;
        }
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= count)
            throw new IndexOutOfRangeException();

        if (index <= 2)
        {
            if (index == 0)
                item0 = item1;

            if (index <= 1)
                item1 = item2;

            item2 = (count >= 4 ? more[0] : default!);
            
            if (count >= 4)
                more.RemoveAt(0);
            count--;
        }
        else if (index >= 3)
        {
            more.RemoveAt(index - 3);
            count--;
        }
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index == -1)
            return false;
        
        RemoveAt(index);
        
        return true;
    }

    public T this[int index]
    {
        get
        {
            if (index < 0 || index > count)
                throw new ArgumentOutOfRangeException();

            if (index == 0)
                return item0;
            
            if (index == 1)
                return item1;

            if (index == 2)
                return item2;
            
            return more[index - 3];
        }
        set
        {
            if (index < 0 || index > count)
                throw new ArgumentOutOfRangeException();
            
            if (index == 0)
                item0 = value;
            else if (index == 1)
                item1 = value;
            else if (index == 2)
                item2 = value;
            else
               more[index - 3] = value;
        }
    }
}