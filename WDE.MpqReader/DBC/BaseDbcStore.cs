using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ProtoZeroSharp;

namespace WDE.MpqReader.DBC;

public abstract unsafe class NativeBaseDbcStore<TKey, TValue> : IDisposable where TKey : unmanaged where TValue : unmanaged, allows ref struct
{
    protected ArenaAllocator allocator;
    private Dictionary<TKey, nuint> lookup = new();
    private TValue* array;
    private int count;

    public int Count => count;

    public NativeBaseDbcStore(uint count)
    {
        this.count = (int)count;
        allocator = new ArenaAllocator();
        var structLength = Unsafe.SizeOf<TValue>();
        array = (TValue*)NativeMemory.AllocZeroed(count, (nuint)structLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Align(int length, int alignment = 8)
    {
        // Fast bitwise rounding up for power-of-2 alignments
        return (length + (alignment - 1)) & ~(alignment - 1);
    }

    public bool TryGetValue(TKey id, out TValue* ptr)
    {
        if (lookup.TryGetValue(id, out var pointer))
        {
            ptr = (TValue*)pointer;
            return true;
        }

        ptr = null;
        return false;
    }

    public ref TValue ElementAt(int index)
    {
        if (index < 0 || index >= count)
            throw new IndexOutOfRangeException();
        return ref *(array + index);
    }

    public struct Enumerator(NativeBaseDbcStore<TKey, TValue> store)
    {
        private int index = -1;
        public TValue* Current => (TValue*)Unsafe.AsPointer(ref store.ElementAt(index));

        public bool MoveNext()
        {
            index++;
            return index < store.count;
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    protected void Set(TKey key, ref TValue value)
    {
        lookup[key] = (nuint)Unsafe.AsPointer(ref value);
    }

    // protected void Set(TKey key, ref TValue value)
    // {
    //     var span = allocator.ReserveContiguousSpan(alignedLength);
    //     ref byte destinationRef = ref MemoryMarshal.GetReference(span);
    //     Unsafe.WriteUnaligned(ref destinationRef, value);
    //
    //     fixed (byte* ptr = span)
    //         lookup[key] = (nuint)ptr;
    // }

    public void Dispose()
    {
        allocator.Free();
    }
}

public abstract class BaseDbcStore<TKey, TVal> where TKey : notnull
{
    protected List<TVal> allValues = new();
    protected Dictionary<TKey, int> store = new();
    public bool TryGetValue(TKey id, out TVal val)
    {
        if (store.TryGetValue(id, out var index))
        {
            val = allValues[index];
            return true;
        }
        val = default;
        return false;
    }

    protected void Set(TKey key, TVal val)
    {
        var nextIndex = allValues.Count;
        if (!store.TryGetValue(key, out var index))
        {
            store[key] = nextIndex;
            allValues.Add(val);
        }
        else
        {
            allValues[index] = val;
        }
    }
    public bool Contains(TKey id) => store.ContainsKey(id);
    public TVal ElementAt(int index) => allValues[index];
    public TVal this[TKey id] => allValues[store[id]];
    public List<TVal>.Enumerator GetEnumerator() => allValues.GetEnumerator();
    public int Count => store.Count;
}