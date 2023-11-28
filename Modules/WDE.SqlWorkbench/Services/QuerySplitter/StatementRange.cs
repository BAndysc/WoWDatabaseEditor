using System;

namespace WDE.SqlWorkbench.Services.QuerySplitter;

internal readonly struct StatementRange : IComparable<StatementRange>, IComparable
{
    public int CompareTo(StatementRange other)
    {
        return Start.CompareTo(other.Start);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        return obj is StatementRange other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(StatementRange)}");
    }

    public static bool operator <(StatementRange left, StatementRange right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(StatementRange left, StatementRange right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(StatementRange left, StatementRange right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(StatementRange left, StatementRange right)
    {
        return left.CompareTo(right) >= 0;
    }

    public readonly int Start;
    public readonly int Line;
    public readonly int Length;

    public StatementRange(int start, int line, int length)
    {
        Start = start;
        Line = line;
        Length = length;
    }
}