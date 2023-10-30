using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CurrencyTypeStore : BaseDbcStore<uint, CurrencyType>
{
    public CurrencyTypeStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CurrencyType(row);
            store[o.Id] = o;
        }
    }
    
    public CurrencyTypeStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new CurrencyType(row);
            store[o.Id] = o;
        }
    }
}