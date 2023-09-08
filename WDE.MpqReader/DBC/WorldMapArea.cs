using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class WorldMapArea
{
    public readonly uint Id;
    public readonly uint ZoneId;
    public readonly int MapId;
    public readonly int DisplayMapId;
    public readonly float Left;
    public readonly float Right;
    public readonly float Top;
    public readonly float Bottom;
    
    public WorldMapArea(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetUInt(0);
        MapId = dbcIterator.GetInt(1);
        ZoneId = dbcIterator.GetUInt(2);
        Left = dbcIterator.GetFloat(4);
        Right = dbcIterator.GetFloat(5);
        Top = dbcIterator.GetFloat(6);
        Bottom = dbcIterator.GetFloat(7);
        DisplayMapId = dbcIterator.GetInt(8);
    }

    public WorldMapArea(IWdcIterator dbcIterator, GameFilesVersion version)
    {
        Id = (uint)dbcIterator.Id;
        MapId = dbcIterator.GetInt("MapID");
        ZoneId = dbcIterator.GetUInt("AreaID");
        Left = dbcIterator.GetFloat("LocLeft");
        Right = dbcIterator.GetFloat("LocRight");
        Top = dbcIterator.GetFloat("LocTop");
        Bottom = dbcIterator.GetFloat("LocBottom");
        DisplayMapId = dbcIterator.GetInt("DisplayMapID");
    }
}