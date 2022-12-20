using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ChrRacesStore : BaseDbcStore<uint, ChrRaces>
{
    public ChrRacesStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ChrRaces(row);
            store[o.Id] = o;
        }
    }
    
    public ChrRacesStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new ChrRaces(row);
            store[o.Id] = o;
        }
    }
}