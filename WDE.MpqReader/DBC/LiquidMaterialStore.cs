using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidMaterialStore : NativeBaseDbcStore<int, LiquidMaterial>
{
    public LiquidMaterialStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new LiquidMaterial(row);
            Set(o.Id, ref o);
        }
    }

    public LiquidMaterialStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new LiquidMaterial(row);
            Set(o.Id, ref o);
        }
    }
}