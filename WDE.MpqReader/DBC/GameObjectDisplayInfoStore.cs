using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class GameObjectDisplayInfoStore : BaseDbcStore<uint, GameObjectDisplayInfo>
{
    public GameObjectDisplayInfoStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new GameObjectDisplayInfo(row);
            store[o.Id] = o;
        }
    }
    
    public GameObjectDisplayInfoStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new GameObjectDisplayInfo(row);
            store[o.Id] = o;
        }
    }
}