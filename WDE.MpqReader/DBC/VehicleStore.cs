using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class VehicleStore : NativeBaseDbcStore<int, Vehicle>
{
    public VehicleStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Vehicle(row, version);
            Set(o.Id, ref o);
        }
    }

    public VehicleStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Vehicle(row, version);
            Set(o.Id, ref o);
        }
    }

    public VehicleStore() : base(0)
    {
    }
}
