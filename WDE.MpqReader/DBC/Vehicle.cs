using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public ref struct Vehicle
{
    public readonly int Id;
    public readonly int Seat0;
    public readonly int Seat1;
    public readonly int Seat2;
    public readonly int Seat3;
    public readonly int Seat4;
    public readonly int Seat5;
    public readonly int Seat6;
    public readonly int Seat7;

    public Vehicle(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetInt(0);
        var seatOffset = version == GameFilesVersion.Mop_5_4_8 ? 7 : 6;
        Seat0 = dbcIterator.GetInt(seatOffset + 0);
        Seat1 = dbcIterator.GetInt(seatOffset + 1);
        Seat2 = dbcIterator.GetInt(seatOffset + 2);
        Seat3 = dbcIterator.GetInt(seatOffset + 3);
        Seat4 = dbcIterator.GetInt(seatOffset + 4);
        Seat5 = dbcIterator.GetInt(seatOffset + 5);
        Seat6 = dbcIterator.GetInt(seatOffset + 6);
        Seat7 = dbcIterator.GetInt(seatOffset + 7);
    }

    public Vehicle(IWdcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.Id;
        Seat0 = dbcIterator.GetUShort("SeatID", 0);
        Seat1 = dbcIterator.GetUShort("SeatID", 1);
        Seat2 = dbcIterator.GetUShort("SeatID", 2);
        Seat3 = dbcIterator.GetUShort("SeatID", 3);
        Seat4 = dbcIterator.GetUShort("SeatID", 4);
        Seat5 = dbcIterator.GetUShort("SeatID", 5);
        Seat6 = dbcIterator.GetUShort("SeatID", 6);
        Seat7 = dbcIterator.GetUShort("SeatID", 7);
    }

    public int GetSeatId(int index)
    {
        return index switch
        {
            0 => Seat0,
            1 => Seat1,
            2 => Seat2,
            3 => Seat3,
            4 => Seat4,
            5 => Seat5,
            6 => Seat6,
            7 => Seat7,
            _ => -1
        };
    }
}