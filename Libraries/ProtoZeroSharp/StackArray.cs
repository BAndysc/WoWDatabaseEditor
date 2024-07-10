using System;
using System.Runtime.CompilerServices;

namespace ProtoZeroSharp;

internal unsafe struct StackArray<T> where T : struct
{
    public const int SizeBytes = 768;

    private readonly int structSize;

    private readonly int maxElements;

    private fixed byte data[SizeBytes];

    private int count;

    public StackArray()
    {
        structSize = Unsafe.SizeOf<T>();
        maxElements = SizeBytes / structSize;
    }

    public ref T this[int index] => ref Unsafe.As<byte, T>(ref data[index * structSize]);

    public void Add(in T t)
    {
        if (maxElements == count)
            throw new InvalidOperationException("StackArray is full.");
        this[count++] = t;
    }

    public ref T Peek()
    {
#if DEBUG
        if (count == 0)
            throw new InvalidOperationException("StackArray is empty.");
#endif
        return ref this[count - 1];
    }

    public void Pop()
    {
#if DEBUG
        if (count == 0)
            throw new InvalidOperationException("StackArray is empty.");
#endif
        count--;
    }

    public T PeekAndPop()
    {
#if DEBUG
        if (count == 0)
            throw new InvalidOperationException("StackArray is empty.");
#endif
        return this[--count];
    }
}