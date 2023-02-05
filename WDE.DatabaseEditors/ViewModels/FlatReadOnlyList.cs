using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace WDE.DatabaseEditors.ViewModels;

public class FlatReadOnlyList<T> : IReadOnlyList<T>
{
    private readonly ObservableCollection<CustomObservableCollection<T>> lists;

    public FlatReadOnlyList(ObservableCollection<CustomObservableCollection<T>> lists)
    {
        this.lists = lists;
    }
    
    public IEnumerator<T> GetEnumerator()
    {
        return lists.SelectMany(l => l).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => lists.Select(l => l.Count).Sum();

    public T this[int index]
    {
        get
        {
            for (int i = 0; i < lists.Count; ++i)
            {
                if (index < lists[i].Count)
                    return lists[i][index];
                index -= lists[i].Count;
            }
            throw new System.IndexOutOfRangeException();
        }
    }
    
    public (int group, int index) GetIndex(T item)
    {
        for (int i = 0; i < lists.Count; ++i)
        {
            int index = lists[i].IndexOf(item);
            if (index != -1)
                return (i, index);
        }
        return (-1, -1);
    }
}