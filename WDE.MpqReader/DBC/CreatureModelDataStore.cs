using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureModelDataStore : BaseDbcStore<uint, CreatureModelData>
{
    public CreatureModelDataStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CreatureModelData(row);
            store[o.Id] = o;
        }
    }
    
    public CreatureModelDataStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CreatureModelData(row);
            store[o.Id] = o;
        }
    }
}