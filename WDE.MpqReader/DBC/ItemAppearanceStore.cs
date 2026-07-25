using WDE.Common.DBC;
using WDE.Common.DBC.Structs;

namespace WDE.MpqReader.DBC;

public class ItemAppearanceStore : NativeBaseDbcStore<uint, ItemAppearance>
{
    public ItemAppearanceStore(IDBC rows) : base(0)
    {

    }

    public ItemAppearanceStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ItemAppearance(row);
            Set(o.Id, ref o);
        }
    }

    public ItemAppearanceStore() : base(0)
    {

    }
}
