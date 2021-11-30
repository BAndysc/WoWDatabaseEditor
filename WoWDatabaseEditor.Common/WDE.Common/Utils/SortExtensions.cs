using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WDE.Common.Utils
{
    public static class SortExtensions
    {
        //  Sorts an IList<T> in place.
        public static void Sort<T>(this IList<T> list, Comparison<T?> comparison)
        {
            ArrayList.Adapter((IList)list).Sort(new ComparisonComparer<T?>(comparison));
        }

        // Sorts in IList<T> in place, when T is IComparable<T>
        public static void Sort<T>(this IList<T> list) where T: IComparable<T>
        {
            Comparison<T?> comparison = (l, r) => l!.CompareTo(r);
            Sort(list, comparison);

        }
    }

    // Wraps a generic Comparison<T> delegate in an IComparer to make it easy
    // to use a lambda expression for methods that take an IComparer or IComparer<T>
    public class ComparisonComparer<T> : IComparer<T>, IComparer
    {
        private readonly Comparison<T?> _comparison;

        public ComparisonComparer(Comparison<T?> comparison)
        {
            _comparison = comparison;
        }

        public int Compare(T? x, T? y)
        {
            return _comparison(x, y);
        }

        public int Compare(object? o1, object? o2)
        {
            return _comparison((T?)o1, (T?)o2);
        }
    }
}