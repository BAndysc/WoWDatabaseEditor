namespace WDE.PacketViewer.Utils.IntervalTrees;

public struct Interval<TKey, TValue>
{
    public readonly TKey start;
    public readonly TKey end;
    public readonly TValue value;

    public Interval(TKey start, TKey end, TValue value)
    {
        this.start = start;
        this.end = end;
        this.value = value;
    }
}