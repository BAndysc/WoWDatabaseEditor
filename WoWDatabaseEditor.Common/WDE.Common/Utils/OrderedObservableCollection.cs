using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace WDE.Common.Utils;

public class OrderedObservableCollection<T> : ObservableCollection<T>
{
    private readonly IComparer<T> comparer;

    public OrderedObservableCollection(IComparer<T> comparer)
    {
        this.comparer = comparer;
    }

    // returns lowest index that would be higher than the element
    // using comparer and binary search
    private int FindLowestIndexHigherThan(T element)
    {
        int min = 0;
        int max = Count;
        while (min < max)
        {
            int mid = (min + max) / 2;
            if (comparer.Compare(this[mid], element) < 0)
                min = mid + 1;
            else
                max = mid;
        }

        return min;
    }

    protected override void InsertItem(int index, T item)
    {
        index = FindLowestIndexHigherThan(item);
        base.InsertItem(index, item);
    }
}