using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class HelmetGeosetVisDataStore : NativeBaseDbcStore<uint, HelmetGeosetVisData>
{
    public HelmetGeosetVisDataStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new HelmetGeosetVisData(row);
            Set(o.Id, ref o);
        }
    }

    public HelmetGeosetVisDataStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new HelmetGeosetVisData(row);
            Set(o.Id, ref o);
        }
    }
}
