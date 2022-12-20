using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidObjectStore : BaseDbcStore<int, LiquidObject>
{
    public LiquidObjectStore() {}

    public LiquidObjectStore(IEnumerable<IDbcIterator> dbcIterator)
    {
        foreach (var iterator in dbcIterator)
        {
            var liquidObject = new LiquidObject(iterator);
            store[liquidObject.Id] = liquidObject;
        }
    }
    
    public LiquidObjectStore(IEnumerable<IWdcIterator> dbcIterator)
    {
        foreach (var iterator in dbcIterator)
        {
            var liquidObject = new LiquidObject(iterator);
            store[liquidObject.Id] = liquidObject;
        }
    }
}