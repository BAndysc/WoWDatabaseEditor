using WDE.Common.DBC;
using WDE.Common.DBC.Structs;

namespace WDE.MpqReader.DBC;

public class ItemAppearanceStore : BaseDbcStore<uint, ItemAppearance>
{
    public ItemAppearanceStore(IEnumerable<IDbcIterator> rows)
    {
        
    }
    
    public ItemAppearanceStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ItemAppearance(row);
            store[o.Id] = o;
        }
    }

    public ItemAppearanceStore()
    {
        
    }
}