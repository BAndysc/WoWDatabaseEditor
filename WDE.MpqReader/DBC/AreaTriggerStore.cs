using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AreaTriggerStore : BaseDbcStore<int, AreaTrigger>
{
    public AreaTriggerStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AreaTrigger(row, version);
            store[o.Id] = o;
        }
    }
    
    public AreaTriggerStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AreaTrigger(row);
            store[o.Id] = o;
        }
    }
}