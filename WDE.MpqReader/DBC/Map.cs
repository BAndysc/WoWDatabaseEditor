using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public enum MapType
{
    Unknown1 = 1,
    Unknown2 = 2,
    Transport = 3
}

public class Map
{
    public readonly int Id;
    public readonly string Directory;
    public readonly string Name;
    public readonly MapType MapType;

    public Map(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetInt(0);
        Directory = dbcIterator.GetString(1);
        if (version == GameFilesVersion.Mop_5_4_8)
        {
            MapType = (MapType)dbcIterator.GetUInt(4);
            Name = dbcIterator.GetString(5);
        }
        else if (version == GameFilesVersion.Cataclysm_4_3_4)
        {
            MapType = (MapType)dbcIterator.GetUInt(4);
            Name = dbcIterator.GetString(6);
        }
        else
        {
            Name = dbcIterator.GetString(5);
            MapType = IsWrathTransportMap(Id) ? MapType.Transport : MapType.Unknown1;
        }
    }

    public Map(IWdcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.Id;
        Directory = dbcIterator.GetString("Directory");
        MapType = (MapType)dbcIterator.GetByte("MapType");
        Name = dbcIterator.GetString("MapName_lang");
    }

    private static bool IsWrathTransportMap(int mapId)
    {
        switch (mapId)
        {
            case 582:
            case 584:
            case 586:
            case 587:
            case 588:
            case 589:
            case 590:
            case 591:
            case 592:
            case 593:
            case 594:
            case 596:
            case 610:
            case 612:
            case 613:
            case 614:
            case 620:
            case 621:
            case 622:
            case 623:
            case 641:
            case 642:
            case 647:
            case 672:
            case 673:
            case 712:
            case 713:
            case 718:
                return true;
            default:
                return false;
        }
    }
    
    private Map()
    {
        Id = -1;
        Directory = "";
        Name = "(null)";
    }

    public static Map Empty => new Map();
}
