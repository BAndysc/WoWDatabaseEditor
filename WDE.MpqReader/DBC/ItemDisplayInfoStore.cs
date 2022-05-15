using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemDisplayInfoStore : IEnumerable<ItemDisplayInfo>
{
    private Dictionary<uint, ItemDisplayInfo> store = new();
    public ItemDisplayInfoStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ItemDisplayInfo(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(uint id) => store.ContainsKey(id);
    public ItemDisplayInfo this[uint id] => store[id];
    public IEnumerator<ItemDisplayInfo> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}