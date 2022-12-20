using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LightStore : BaseDbcStore<uint, Light>
{
    public LightStore(IEnumerable<IDbcIterator> rows, LightParamStore lightParamStore)
    {
        foreach (var row in rows)
        {
            var o = new Light(row, lightParamStore);
            store[o.Id] = o;
        }
    }
    
    public LightStore(IEnumerable<IWdcIterator> rows, LightParamStore lightParamStore)
    {
        foreach (var row in rows)
        {
            var o = new Light(row, lightParamStore);
            store[o.Id] = o;
        }
    }
}