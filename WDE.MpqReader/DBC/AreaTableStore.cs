using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AreaTableStore : BaseDbcStore<uint, AreaTable>
{
    public AreaTableStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new AreaTable(row);
            store[o.Id] = o;
        }
    }
}