using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharacterFacialHairStyles
{
    // public readonly uint Id; // deosn't exist in dbc
    public readonly int RaceID;
    public readonly int SexId;
    public readonly int VariationId;
    public readonly int Geoset1;
    public readonly int Geoset2;
    public readonly int Geoset3;
    public readonly int Geoset4;
    public readonly int Geoset5;

    public CharacterFacialHairStyles(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        // Id = 0;
        int i = version == GameFilesVersion.Cataclysm_4_3_4 ? 1 : 0;
        RaceID = dbcIterator.GetInt(i++);
        SexId = dbcIterator.GetInt(i++);
        VariationId = dbcIterator.GetInt(i++);
        Geoset1 = dbcIterator.GetInt(i++);
        Geoset2 = dbcIterator.GetInt(i++);
        Geoset3 = dbcIterator.GetInt(i++);
        Geoset4 = dbcIterator.GetInt(i++);
        Geoset5 = dbcIterator.GetInt(i++);
    }
    
    public CharacterFacialHairStyles(IWdcIterator dbcIterator, GameFilesVersion version)
    {
        // Id = 0;
        RaceID = dbcIterator.GetByte("RaceID");
        SexId = dbcIterator.GetByte("SexID");
        VariationId = dbcIterator.GetByte("VariationID");
        Geoset1 = dbcIterator.GetInt("Geoset", 0);
        Geoset2 = dbcIterator.GetInt("Geoset", 1);
        Geoset3 = dbcIterator.GetInt("Geoset", 2);
        Geoset4 = dbcIterator.GetInt("Geoset", 3);
        Geoset5 = dbcIterator.GetInt("Geoset", 4);
    }

    private CharacterFacialHairStyles()
    {
        // Id = 0;
        RaceID = 0;
        SexId = 0;
        VariationId = 0;
        Geoset1 = 0;
        Geoset2 = 0;
        Geoset3 = 0;
        Geoset4 = 0;
        Geoset5 = 0;
    }

    public static CharacterFacialHairStyles Empty => new CharacterFacialHairStyles();
}