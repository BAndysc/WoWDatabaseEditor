using System;
using System.Runtime.Versioning;

namespace WDE.PacketViewer.Utils.IntervalTrees;

[RequiresPreviewFeatures]
public class OptimizedIntervalTree<TKey, TValue> : IIntervalTree<TKey, TValue> where TKey : INumber<TKey> where TValue : struct
{
    private Interval<TKey, TValue> value;
    private IIntervalTree<TKey, TValue>? values;

    public OptimizedIntervalTree(TKey start, TKey ending, TValue value)
    {
        this.value = new Interval<TKey, TValue>(start, ending, value);
    }
    
    public Interval<TKey, TValue>? Query(TKey at)
    {
        if (values != null)
            return values.Query(at);
        return value.start <= at && at <= value.end ? value : null;
    }

    public Interval<TKey, TValue>? QueryBefore(TKey at)
    {
        if (values != null)
            return values.QueryBefore(at);
        return value.end < at ? value : null;
    }

    public Interval<TKey, TValue>? QueryAfter(TKey at)
    {
        if (values != null)
            return values.QueryAfter(at);
        return value.start > at ? value : null;
    }

    public void Add(TKey @from, TKey to, TValue value)
    {
        if (values == null)
        {
            values = new IntervalTree<TKey, TValue>();
            values.Add(this.value.start, this.value.end, this.value.value);
        }
        values.Add(@from, to, value);
    }
}