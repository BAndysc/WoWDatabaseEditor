using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoZeroSharp;

[DebuggerTypeProxy(typeof(UnmanagedArrayDebugView<>))]
public readonly unsafe struct UnmanagedArray<T> where T : unmanaged
{
    public readonly int Length;
    private readonly T* data;

    public UnmanagedArray(int length, T* data)
    {
        Length = length;
        this.data = data;
    }

    public ref T this[int index]
    {
        get
        {
#if DEBUG
            if ((uint)index >= (uint)Length)
            {
                throw new IndexOutOfRangeException();
            }
#endif
            return ref Unsafe.AsRef<T>(data[index]);
        }
    }

    public int Count => Length;

    public Span<T> AsSpan() => new Span<T>(data, Length);

    public static implicit operator ReadOnlySpan<T>(UnmanagedArray<T> array) => array.AsSpan();

    public static UnmanagedArray<T> AllocArray(int maxCount, ref ArenaAllocator memory)
    {
        var data = memory.TakeContiguousSpan(maxCount * sizeof(T));
        return new UnmanagedArray<T>(maxCount, (T*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(data)));
    }

    public T* GetPointer(int index)
    {
        return data + index;
    }
}