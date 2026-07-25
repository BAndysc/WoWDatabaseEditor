using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public unsafe class ItemModifiedAppearanceStore : NativeBaseDbcStore<uint, ItemModifiedAppearance>
{
    private Dictionary<uint, uint> byItems = new();

    public ItemModifiedAppearanceStore(IDBC rows) : base(0)
    {

    }

    public ItemModifiedAppearanceStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ItemModifiedAppearance(row);
            Set(o.Id, ref o);
            if (!byItems.TryGetValue(o.ItemId, out var list))
            {
                list = byItems[o.ItemId] = o.Id;
            }
        }
    }

    public bool TryGetByItem(uint itemId, out uint firstId)
    {
        return byItems.TryGetValue(itemId, out firstId);
    }

    public ItemModifiedAppearanceStore() : base(0)
    {

    }
}
