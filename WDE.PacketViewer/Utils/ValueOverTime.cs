using System;
using System.Collections.Generic;

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
