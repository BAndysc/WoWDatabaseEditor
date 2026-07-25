using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CurrencyTypeStore : NativeBaseDbcStore<uint, CurrencyType>
{
    public CurrencyTypeStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CurrencyType(row);
            Set(o.Id, ref o);
        }
    }

    public CurrencyTypeStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CurrencyType(row);
            Set(o.Id, ref o);
        }
    }
}
