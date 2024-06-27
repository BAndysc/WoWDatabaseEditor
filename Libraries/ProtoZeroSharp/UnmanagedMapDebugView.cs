using System.Collections.Generic;
using System.Diagnostics;

namespace ProtoZeroSharp;

public class UnmanagedMapDebugView<TKey, TValue> where TKey : unmanaged where TValue : unmanaged
{
    private readonly UnmanagedMap<TKey, TValue> _map;

    public UnmanagedMapDebugView(UnmanagedMap<TKey, TValue> map)
    {
        _map = map;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public KeyValuePair<TKey, TValue>[] Items
    {
        get
        {
            _map.GetUnderlyingArrays(out var keys, out var values);
            KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[_map.Length];
            for (int i = 0; i < _map.Length; i++)
            {
                array[i] = new KeyValuePair<TKey, TValue>(keys[i], values[i]);
            }
            return array;
        }
    }
}