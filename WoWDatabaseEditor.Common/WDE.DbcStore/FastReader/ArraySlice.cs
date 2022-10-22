using System;

namespace WDE.DbcStore.FastReader;

internal readonly struct ArraySlice<T>
{
    private readonly T[] array;
    private readonly int offset;
    private readonly int length;

    public ArraySlice(T[] array, int offset, int length)
    {
        this.array = array;
        this.offset = offset;
        this.length = length;
    }

    public ReadOnlySpan<T> Span => length <= 0 ? ReadOnlySpan<T>.Empty : new ReadOnlySpan<T>(array, offset, length);
    
    public int Start => offset;
    
    public int Length => length;

    public ArraySlice<T> Skip(int count)
    {
        return new ArraySlice<T>(array, offset + count, length - count);
    }
}