using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AreaTriggerStore : IEnumerable<AreaTrigger>
{
    private Dictionary<int, AreaTrigger> store = new();
    public AreaTriggerStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new AreaTrigger(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(int id) => store.ContainsKey(id);
    public AreaTrigger this[int id] => store[id];
    public IEnumerator<AreaTrigger> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}