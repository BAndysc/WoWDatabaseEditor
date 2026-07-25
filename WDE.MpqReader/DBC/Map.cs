using ProtoZeroSharp;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public enum MapType
{
    Unknown1 = 1,
    Unknown2 = 2,
    Transport = 3
}

public ref struct Map
{
    public readonly int Id;
    public readonly ManagedString Directory;
    public readonly Utf8NativeString Name;
    public readonly MapType MapType;

    public Map(IDbcIterator dbcIterator, GameFilesVersion version, ref ArenaAllocator allocator)
    {
        Id = dbcIterator.GetInt(0);
        Directory = ManagedString.Create(dbcIterator.GetString(1));
        if (version == GameFilesVersion.Mop_5_4_8)
        {
            MapType = (MapType)dbcIterator.GetUInt(4);
            Name = allocator.AllocString(dbcIterator.GetUtf8String(5));
        }
        else if (version == GameFilesVersion.Cataclysm_4_3_4)
        {
            MapType = (MapType)dbcIterator.GetUInt(4);
            Name = allocator.AllocString(dbcIterator.GetUtf8String(6));
        }
        else
        {
            Name = allocator.AllocString(dbcIterator.GetUtf8String(5));
            MapType = IsWrathTransportMap(Id) ? MapType.Transport : MapType.Unknown1;
        }
    }

    public Map(IWdcIterator dbcIterator, GameFilesVersion version, ref ArenaAllocator allocator)
    {
        Id = dbcIterator.Id;
        Directory = ManagedString.Create(dbcIterator.GetString("Directory"));
        MapType = (MapType)dbcIterator.GetByte("MapType");
        Name = allocator.AllocString(dbcIterator.GetString("MapName_lang"));
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

    public Map()
    {
        Id = -1;
        Directory = ManagedString.Empty;
        Name = Utf8NativeString.Null;
    }

    public static Map Empty => new Map();
}
