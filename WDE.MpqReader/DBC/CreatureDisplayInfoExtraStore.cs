using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoExtraStore : BaseDbcStore<uint, CreatureDisplayInfoExtra>
{
    public CreatureDisplayInfoExtraStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CreatureDisplayInfoExtra(row);
            store[o.Id] = o;
        }
    }
    
    public CreatureDisplayInfoExtraStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CreatureDisplayInfoExtra(row);
            store[o.Id] = o;
        }
    }
}