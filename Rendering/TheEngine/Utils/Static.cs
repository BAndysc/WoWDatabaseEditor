using System.Runtime.InteropServices;

namespace TheEngine.Utils;

public readonly record struct StaticReference
{
    private readonly int index;
    private readonly int generation;

    public StaticReference(int index, int generation)
    {
        this.index = index + 1;
        this.generation = generation;
    }

    public int Index => index - 1;
    public int Generation => generation;

    public bool IsEmpty => index == default;

    public void Free()
    {
        Static.Release(this);
    }

    public IntPtr AsIntPtr()
    {
        #if DEBUG
        if (Marshal.SizeOf<IntPtr>() < 8)
        {
            throw new Exception("Really? using 32-bit OS?");
        }
        #endif
        return new IntPtr((long)Index << 32 | (long)Generation);
    }

    public static StaticReference FromIntPtr(IntPtr intPtr)
    {
        var index = (long)intPtr >> 32;
        var generation = intPtr & 0xFFFFFFFF;
        return new StaticReference((int)index, (int)generation);
    }
}

public static class Static
{
    private const int InitSize = 20_000;
    public static readonly List<(object?, int generation)> values;
    private static readonly Stack<StaticReference> free = new();
    internal static int MainThreadId = Environment.CurrentManagedThreadId;

    static Static()
    {
        values = new List<(object?, int generation)>(InitSize);
        for (int i = 0; i < InitSize; ++i)
        {
            values.Add(default);
            free.Push(new StaticReference(i, 1));
        }
    }
    
    public static StaticReference GetStaticReference(this object value)
    {
        return Keep(value);
    }

    public static StaticReference Keep(object value)
    {
#if DEBUG
        if (Environment.CurrentManagedThreadId != MainThreadId)
            throw new InvalidOperationException($"Can't access Static References from outside of the main thread");
#endif
        if (free.Count != 0)
        {
            StaticReference id = free.Pop();
            values[id.Index] = (value, id.Generation);
            return id;
        }

        int result = values.Count;
        values.Add((value, 0));
        return new StaticReference(result, 0);
    }

    public static void Release(StaticReference id)
    {
#if DEBUG
        if (Environment.CurrentManagedThreadId != MainThreadId)
            throw new InvalidOperationException($"Can't access Static References from outside of the main thread");
#endif
        if (id.Index >= values.Count)
            return;

        values[id.Index] = default;
        free.Push(new StaticReference(id.Index, id.Generation + 1));
    }

    public static bool TryGet(StaticReference id, out object? value)
    {
#if DEBUG
        if (Environment.CurrentManagedThreadId != MainThreadId)
            throw new InvalidOperationException($"Can't access Static References from outside of the main thread");
#endif
        if (id.Index >= values.Count)
        {
            value = null;
            return false;
        }

        var val = values[id.Index];

        if (val.generation != id.Generation)
        {
            value = false;
            return false;
        }

        value = values[id.Index].Item1;
        return value != null;
    }
}