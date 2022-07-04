using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class ItemDisplayInfoStore : BaseDbcStore<uint, ItemDisplayInfo>
{
    public ItemDisplayInfoStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new ItemDisplayInfo(row, version);
            store[o.Id] = o;
        }
    }
    
    public ItemDisplayInfoStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new ItemDisplayInfo(row, version);
            store[o.Id] = o;
        }
    }
}