using System;
using System.Collections.Generic;

namespace WDE.DatabaseEditors.Data.Structs;

public class NullableOrderedEquality<T> : IEqualityComparer<IReadOnlyCollection<T>>
{
    public static readonly NullableOrderedEquality<T> Default = new();

    public bool Equals(IReadOnlyCollection<T>? x, IReadOnlyCollection<T>? y)
    {
        if (x == null && y == null)
            return true;
        if (x == null && y!.Count == 0)
            return true;
        if (y == null && x!.Count == 0)
            return true;
        if (x == null && y != null || y == null && x != null)
            return false;
        return Generator.Equals.OrderedEqualityComparer<T>.Default.Equals(x!, y!);
    }

    public int GetHashCode(IReadOnlyCollection<T>? obj)
    {
        if (obj == null)
            return 0;
        return global::Generator.Equals.OrderedEqualityComparer<T>.Default.GetHashCode(obj);
    }
}