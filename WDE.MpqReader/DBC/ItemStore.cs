using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemStore : NativeBaseDbcStore<uint, Item>
{
    public ItemStore(IDBC rows, ItemModifiedAppearanceStore itemModifiedAppearanceStore, ItemAppearanceStore itemAppearanceStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Item(row);
            Set(o.Id, ref o);
        }
    }

    public ItemStore(IWDC rows, ItemModifiedAppearanceStore itemModifiedAppearanceStore, ItemAppearanceStore itemAppearanceStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Item(row, itemModifiedAppearanceStore, itemAppearanceStore);
            Set(o.Id, ref o);
        }
    }
}