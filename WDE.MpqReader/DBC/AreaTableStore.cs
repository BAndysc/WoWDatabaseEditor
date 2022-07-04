using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AreaTableStore : BaseDbcStore<uint, AreaTable>
{
    public AreaTableStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AreaTable(row, version);
            store[o.Id] = o;
        }
    }
    
    public AreaTableStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AreaTable(row);
            store[o.Id] = o;
        }
    }
}