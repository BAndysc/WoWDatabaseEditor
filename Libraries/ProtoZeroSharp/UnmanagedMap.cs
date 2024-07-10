using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoZeroSharp;

[DebuggerTypeProxy(typeof(UnmanagedMapDebugView<,>))]
public readonly unsafe struct UnmanagedMap<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
{
    public readonly int Length;
    private readonly TKey* keys;
    private readonly TValue* values;

    private static TValue defaultValue = default;

    public UnmanagedMap(int length, TKey* keys, TValue* values)
    {
        Length = length;
        this.keys = keys;
        this.values = values;
    }

    public ref TValue TryGetValue(TKey key, out bool found)
    {
        for (int i = 0; i < Length; ++i)
        {
            if (Unsafe.AsRef<TKey>(keys[i]).Equals(key))
            {
                found = true;
                return ref Unsafe.AsRef<TValue>(values[i]);
            }
        }

        found = false;
        return ref defaultValue;
    }

    public void GetUnderlyingArrays(out Span<TKey> keysSpan, out Span<TValue> valuesSpan)
    {
        keysSpan = new Span<TKey>(keys, Length);
        valuesSpan = new Span<TValue>(values, Length);
    }

    public static UnmanagedMap<TKey, TValue> AllocMap(int maxCount, ref ArenaAllocator memory)
    {
        var keys = memory.TakeContiguousSpan(maxCount * sizeof(TKey));
        var values = memory.TakeContiguousSpan(maxCount * sizeof(TValue));
        return new UnmanagedMap<TKey, TValue>(maxCount, (TKey*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(keys)),
            (TValue*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(values)));
    }
}