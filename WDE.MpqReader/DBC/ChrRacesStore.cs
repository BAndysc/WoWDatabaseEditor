using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ChrRacesStore : NativeBaseDbcStore<uint, ChrRaces>
{
    public ChrRacesStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ChrRaces(row, ref allocator);
            Set(o.Id, ref o);
        }
    }

    public ChrRacesStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ChrRaces(row, ref allocator);
            Set(o.Id, ref o);
        }
    }
}
