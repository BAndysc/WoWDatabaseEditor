using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace WDE.PacketViewer.Utils.IntervalTrees;

[RequiresPreviewFeatures]
internal class IntervalTree<TKey, TValue> : IIntervalTree<TKey, TValue> where TKey : INumber<TKey> where TValue : struct
{
    private List<Interval<TKey, TValue>> intervals = new ();
    
    public void Add(TKey @from, TKey to, TValue value)
    {
        Debug.Assert(intervals.Count == 0 || intervals[^1].end < from);
        intervals.Add(new (@from, to, value));
    }

    public Interval<TKey, TValue>? Query(TKey at)
    {
        int index = intervals.BinarySearchOrGreater(at, ComparerEnd);

        if (index >= 0)
            return intervals[index];
        else
        {
            index = ~index;
            if (index == intervals.Count)
                return null;
            
            if (intervals[index].start <= at && at <= intervals[index].end)
                return intervals[index];
            else
                return null;
        }
    }

    public Interval<TKey, TValue>? QueryBefore(TKey at)
    {
        int index = intervals.BinarySearchOrGreater(at, ComparerEnd);

        if (index >= 0)
            return index - 1 >= 0 ? intervals[index - 1] : null;
        else
        {
            index = ~index;
            if (index == intervals.Count)
                return index - 1 >= 0 ? intervals[index - 1] : null;

            if (intervals[index].start <= at && at <= intervals[index].end)
            {
                return index - 1 >= 0 ? intervals[index - 1] : null;
            }
            else if (index > 0 && intervals[index - 1].end < at)
                return intervals[index - 1];
            else if (index > 0 && intervals[index - 1].start <= at && at <= intervals[index - 1].end)
            {
                return index - 2 >= 0 ? intervals[index - 2] : null;
            }
            else
                return null;
        }
    }

    public Interval<TKey, TValue>? QueryAfter(TKey at)
    {
        int index = intervals.BinarySearchOrGreater(at, ComparerEnd);

        if (index >= 0)
            return index + 1 <= intervals.Count - 1 ? intervals[index + 1] : null;
        else
        {
            index = ~index;
            if (index == intervals.Count)
                return null;

            if (intervals[index].start <= at && at <= intervals[index].end)
            {
                return index + 1 <= intervals.Count - 1 ? intervals[index + 1] : null;
            }
            else if (intervals[index].start > at)
                return intervals[index];
            else
                return null;
        }
    }

    private int ComparerEnd(Interval<TKey, TValue> interval, TKey value)
    {
        return interval.end.CompareTo(value);
    }
}