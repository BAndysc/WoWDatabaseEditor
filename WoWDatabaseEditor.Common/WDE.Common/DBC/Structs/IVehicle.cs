namespace WDE.Common.DBC.Structs;

public interface IVehicle
{
    public const int MaxSeats = 8;

    uint Id { get; }
    uint[] Seats { get; } // @todo: replace with https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#inline-arrays in net8
}

public class Vehicle : IVehicle
{
    public uint Id { get; init; }
    public uint[] Seats { get; } = new uint[IVehicle.MaxSeats];
}