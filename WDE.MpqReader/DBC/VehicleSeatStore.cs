using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class VehicleSeatStore : NativeBaseDbcStore<int, VehicleSeat>
{
    public VehicleSeatStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new VehicleSeat(row, version);
            Set(o.Id, ref o);
        }
    }

    public VehicleSeatStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new VehicleSeat(row, version);
            Set(o.Id, ref o);
        }
    }

    public VehicleSeatStore() : base(0)
    {
    }
}