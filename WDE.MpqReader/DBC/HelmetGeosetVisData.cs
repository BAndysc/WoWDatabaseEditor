using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class HelmetGeosetVisData
{
    public readonly uint Id;
    public readonly uint HairFlags;
    public readonly uint Facial1Flags; // (Beard, Tusks)
    public readonly uint Facial2Flags; // (Earrings)
    public readonly uint Facial3Flags; // See ChrRaces, column 24 to 26 for information on what is what.
    public readonly uint EarsFlags;
    public readonly uint MiscFlags;
    public readonly uint EyesFlags;

    public HelmetGeosetVisData(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        HairFlags = dbcIterator.GetUInt(1);
        Facial1Flags = dbcIterator.GetUInt(2);
        Facial2Flags = dbcIterator.GetUInt(3);
        Facial3Flags = dbcIterator.GetUInt(4);
        EarsFlags = dbcIterator.GetUInt(5);
        MiscFlags = dbcIterator.GetUInt(6);
        EyesFlags = dbcIterator.GetUInt(7);
    }
    
    public HelmetGeosetVisData(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        HairFlags = (uint)dbcIterator.GetInt("HideGeoset", 0);
        Facial1Flags = (uint)dbcIterator.GetInt("HideGeoset", 1);
        Facial2Flags = (uint)dbcIterator.GetInt("HideGeoset", 2);
        Facial3Flags = (uint)dbcIterator.GetInt("HideGeoset", 3);
        EarsFlags = (uint)dbcIterator.GetInt("HideGeoset", 4);
        MiscFlags = (uint)dbcIterator.GetInt("HideGeoset", 5);
        EyesFlags = (uint)dbcIterator.GetInt("HideGeoset", 6);
    }

    private HelmetGeosetVisData()
    {
        Id = 0;
        HairFlags = 0;
        Facial1Flags = 0;
        Facial2Flags = 0;
        Facial3Flags = 0;
        EarsFlags = 0;
        MiscFlags = 0;
        EyesFlags = 0;
    }

    public static HelmetGeosetVisData Empty => new HelmetGeosetVisData();
}