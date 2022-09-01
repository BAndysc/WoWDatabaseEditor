using System.Collections;

namespace WDE.MpqReader.DBC;

public abstract class BaseDbcStore<TKey, TVal> : IEnumerable<TVal> where TKey : notnull
{
    protected Dictionary<TKey, TVal> store = new();
    public bool TryGetValue(TKey id, out TVal val) => store.TryGetValue(id, out val!);
    public bool Contains(TKey id) => store.ContainsKey(id);
    public TVal this[TKey id] => store[id];
    public IEnumerator<TVal> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    public int Count => store.Count;
}