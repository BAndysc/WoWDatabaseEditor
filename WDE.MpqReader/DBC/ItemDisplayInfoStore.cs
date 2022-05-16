using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemDisplayInfoStore : BaseDbcStore<uint, ItemDisplayInfo>
{
    public ItemDisplayInfoStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ItemDisplayInfo(row);
            store[o.Id] = o;
        }
    }
}