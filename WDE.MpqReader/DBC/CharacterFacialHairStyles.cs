using WDE.Common.DBC;

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

    public CharacterFacialHairStyles(IDbcIterator dbcIterator)
    {
        // Id = 0;
        RaceID = dbcIterator.GetInt(0);
        SexId = dbcIterator.GetInt(1);
        VariationId = dbcIterator.GetInt(2);
        Geoset1 = dbcIterator.GetInt(3);
        Geoset2 = dbcIterator.GetInt(4);
        Geoset3 = dbcIterator.GetInt(5);
        Geoset4 = dbcIterator.GetInt(6);
        Geoset5 = dbcIterator.GetInt(7);
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