using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidMaterialStore : BaseDbcStore<int, LiquidMaterial>
{
    public LiquidMaterialStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LiquidMaterial(row);
            store[o.Id] = o;
        }
    }
    
    public LiquidMaterialStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LiquidMaterial(row);
            store[o.Id] = o;
        }
    }
}