using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class VehicleStore : BaseDbcStore<int, Vehicle>
{
    public VehicleStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new Vehicle(row, version);
            store[o.Id] = o;
        }
    }

    public VehicleStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new Vehicle(row, version);
            store[o.Id] = o;
        }
    }

    public VehicleStore()
    {
    }
}