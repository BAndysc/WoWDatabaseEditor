using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemStore : BaseDbcStore<uint, Item>
{
    public ItemStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new Item(row);
            store[o.Id] = o;
        }
    }
    
    public ItemStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new Item(row);
            store[o.Id] = o;
        }
    }
}