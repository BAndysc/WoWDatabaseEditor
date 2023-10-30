using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemModifiedAppearanceStore : BaseDbcStore<uint, ItemModifiedAppearance>
{
    private Dictionary<uint, List<ItemModifiedAppearance>> byItems = new();
    
    public ItemModifiedAppearanceStore(IEnumerable<IDbcIterator> rows)
    {
        
    }
    
    public ItemModifiedAppearanceStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ItemModifiedAppearance(row);
            store[o.Id] = o;
            if (!byItems.TryGetValue(o.ItemId, out var list))
                list = byItems[o.ItemId] = new List<ItemModifiedAppearance>();
            list.Add(o);
        }
    }
    
    public bool TryGetByItem(uint itemId, out IReadOnlyList<ItemModifiedAppearance> list)
    {
        if (byItems.TryGetValue(itemId, out var list2))
        {
            list = list2;
            return true;
        }

        list = Array.Empty<ItemModifiedAppearance>();
        return false;
    }

    public ItemModifiedAppearanceStore()
    {
        
    }
}