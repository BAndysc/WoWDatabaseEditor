using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoStore : BaseDbcStore<uint, CreatureDisplayInfo>
{
    public CreatureDisplayInfoStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CreatureDisplayInfo(row);
            store[o.Id] = o;
        }
    }
}