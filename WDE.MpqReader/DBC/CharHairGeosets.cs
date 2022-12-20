using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharHairGeosets
{
    public readonly uint Id;
    public readonly int RaceID;
    public readonly int SexId;
    public readonly int VariationId;
    public readonly int VariationType;
    public readonly int GeosetId;
    public readonly int GeosetType;
    public readonly int ShowScalp; // bald

    public CharHairGeosets(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetUInt(0);
        RaceID = dbcIterator.GetInt(1);
        SexId = dbcIterator.GetInt(2);
        VariationId = dbcIterator.GetInt(3);
        if (version == GameFilesVersion.Mop_5_4_8)
        {
            VariationType = dbcIterator.GetInt(4);
            GeosetId = dbcIterator.GetInt(5);
            GeosetType = dbcIterator.GetInt(6);
            ShowScalp = dbcIterator.GetInt(7);
        }
        else
        {
            GeosetId = dbcIterator.GetInt(4);
            ShowScalp = dbcIterator.GetInt(5);
        }
    }
    
    public CharHairGeosets(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        RaceID = dbcIterator.GetByte("RaceID");
        SexId = dbcIterator.GetByte("SexID");
        VariationId = dbcIterator.GetByte("VariationID");
        VariationType = dbcIterator.GetByte("VariationType");
        GeosetId = dbcIterator.GetByte("GeosetID");
        GeosetType = dbcIterator.GetByte("GeosetType");
        ShowScalp = dbcIterator.GetByte("Showscalp");
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