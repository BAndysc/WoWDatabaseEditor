using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AreaTableStore : IEnumerable<AreaTable>
{
    private Dictionary<uint, AreaTable> store = new();
    public AreaTableStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new AreaTable(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public AreaTable this[uint id] => store[id];
    public IEnumerator<AreaTable> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}