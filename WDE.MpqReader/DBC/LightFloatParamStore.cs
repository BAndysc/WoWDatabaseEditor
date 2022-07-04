using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightFloatParamStore : BaseDbcStore<uint, LightFloatParam>
{
    public LightFloatParamStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new LightFloatParam(row);
            store[o.Id] = o;
        }
    }
    
    public LightFloatParamStore()
    {

    }
}