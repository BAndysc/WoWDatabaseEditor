using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class HelmetGeosetVisDataStore : BaseDbcStore<uint, HelmetGeosetVisData>
{
    public HelmetGeosetVisDataStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new HelmetGeosetVisData(row);
            store[o.Id] = o;
        }
    }
    
    public HelmetGeosetVisDataStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new HelmetGeosetVisData(row);
            store[o.Id] = o;
        }
    }
}