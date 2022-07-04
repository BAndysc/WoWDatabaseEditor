using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoStore : BaseDbcStore<uint, CreatureDisplayInfo>
{
    public CreatureDisplayInfoStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new CreatureDisplayInfo(row, version);
            store[o.Id] = o;
        }
    }
    
    public CreatureDisplayInfoStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new CreatureDisplayInfo(row, version);
            store[o.Id] = o;
        }
    }
}