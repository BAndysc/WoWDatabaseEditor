using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharHairGeosets
{
    public readonly uint Id;
    public readonly int RaceID;
    public readonly int SexId;
    public readonly int VariationId;
    public readonly int GeosetId;
    public readonly int ShowScalp; // bald

    public CharHairGeosets(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        RaceID = dbcIterator.GetInt(1);
        SexId = dbcIterator.GetInt(2);
        VariationId = dbcIterator.GetInt(3);
        GeosetId = dbcIterator.GetInt(4);
        ShowScalp = dbcIterator.GetInt(5);
    }

    private CharHairGeosets()
    {
        Id = 0;
        RaceID = 0;
        SexId = 0;
        VariationId = 0;
        GeosetId = 0;
        ShowScalp = 0;
    }

    public static CharHairGeosets Empty => new CharHairGeosets();
}