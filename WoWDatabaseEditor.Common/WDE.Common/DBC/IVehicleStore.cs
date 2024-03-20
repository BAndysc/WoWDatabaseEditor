using System.Collections.Generic;
using WDE.Common.DBC.Structs;

namespace WDE.Common.DBC;

public interface IVehicleStore
{
    IReadOnlyList<IVehicle> Vehicles { get; }
    IVehicle? GetVehicleById(uint id);
    IVehicle? GetVehicleBySeatId(uint id);
}
