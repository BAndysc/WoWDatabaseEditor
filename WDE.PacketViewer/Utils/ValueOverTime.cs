using System;
using System.Collections;
using System.Collections.Generic;
using WDE.Common.Utils;

namespace WDE.PacketViewer.Utils;


public class ValueOverTime<T> where T : unmanaged
{
    private bool set;
    public T Value { get; private set; }

    public bool HasValue => set;
    public bool HasMultiValue { get; private set; }

    public ValueOverTime()
    {
        Value = default!;
    }

    public ValueOverTime(T value)
    {
        Value = value;
        set = true;
    }

    public T? AsNullable => HasValue ? Value : null;
    
    public void Update(T value)
    {
        if (!set)
        {
            Value = value;
            set = true;
            return;
        }
        if (EqualityComparer<T>.Default.Equals(Value, value))
            return;
        this.Value = value;
        HasMultiValue = true;
    }
}

public struct TrackedUniqueValueOverTime<T> : IEnumerable<T> where T : unmanaged
{
    private SmallList<T> values;

    public bool HasValue => values.Count > 0;
    public bool HasMultiValue => values.Count > 1;

    public TrackedUniqueValueOverTime()
    {
        values = new SmallList<T>();
    }

    public TrackedUniqueValueOverTime(T value)
    {
        values = new SmallList<T>(value);
    }
    
    public void Update(T value)
    {
        for (int i = 0; i < values.Count; ++i)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], value))
                return;
        }
        values.Add(value);
    }
    
    public T this[int index] => values[index];
    
    public int Count => values.Count;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var v in values)
            yield return v;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
