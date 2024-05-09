using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Models;

namespace WDE.DatabaseEditors.Parameters;

public class VehicleSeatIdParameter : ICustomPickerContextualParameter<long>
{
    private readonly IItemFromListProvider itemFromListProvider;
    private readonly IVehicleStore vehicleStore;

    public string? Prefix => null;

    public bool HasItems => true;

    public Dictionary<long, SelectOption>? Items => null;

    public VehicleSeatIdParameter(
        IItemFromListProvider itemFromListProvider,
        IVehicleStore vehicleStore)
    {
        this.itemFromListProvider = itemFromListProvider;
        this.vehicleStore = vehicleStore;
    }

    public string ToString(long value)
    {
        if (vehicleStore.GetVehicleBySeatId((uint)value) is { } vehicle)
        {
            var seatIndex = vehicle.Seats.IndexOf((uint)value);

            if (seatIndex == -1)
                return $"{value} (unknown seat)";

            return $"{value} (seat #{seatIndex})";
        }

        return $"{value} (unknown vehicle)";
    }

    public async Task<(long, bool)> PickValue(long value, object context)
    {
        if (context is not DatabaseEntity entity)
        {
            return (0, false);
        }

        var vehicleId = entity.GetTypedValueOrThrow<long>(new ColumnFullName(null, "vehicleId"));
        if (vehicleStore.GetVehicleById((uint)vehicleId) is { } vehicle)
        {
            Dictionary<long, SelectOption> options = vehicle
                .Seats
                .Select((seatId, idx) => (seatId, idx))
                .Where(pair => pair.seatId != 0)
                .ToDictionary(pair => (long)pair.seatId, pair => new SelectOption($"Seat #{pair.idx}"));

            var picked = await itemFromListProvider.GetItemFromList(options, false, value, "Select seat");
            return (picked ?? 0, picked.HasValue);
        }
        else
        {
            var picked = await itemFromListProvider.GetItemFromList(null, false, value, "Select seat");
            return (picked ?? 0, picked.HasValue);
        }
    }
}