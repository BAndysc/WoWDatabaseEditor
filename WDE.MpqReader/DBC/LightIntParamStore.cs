using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightIntParamStore : BaseDbcStore<uint, LightIntParam>
{
    public LightIntParamStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LightIntParam(row);
            store[o.Id] = o;
        }
    }
    
    public LightIntParamStore()
    {
    }
}