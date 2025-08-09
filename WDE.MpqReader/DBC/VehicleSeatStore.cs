using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class VehicleSeatStore : BaseDbcStore<int, VehicleSeat>
{
    public VehicleSeatStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new VehicleSeat(row, version);
            store[o.Id] = o;
        }
    }

    public VehicleSeatStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new VehicleSeat(row, version);
            store[o.Id] = o;
        }
    }

    public VehicleSeatStore()
    {
    }
}