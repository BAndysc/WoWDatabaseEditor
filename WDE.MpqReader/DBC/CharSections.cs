using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharSections
{
    public readonly uint Id;
    public readonly int RaceID;
    public readonly int SexId;
    public readonly int BaseSection;
    public readonly string TextureName1;
    public readonly string TextureName2;
    public readonly string TextureName3;
    public readonly int Flags;
    public readonly int VariationIndex;
    public readonly int ColorIndex;

    public CharSections(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        RaceID = dbcIterator.GetInt(1);
        SexId = dbcIterator.GetInt(2);
        BaseSection = dbcIterator.GetInt(3);
        TextureName1 = dbcIterator.GetString(4);
        TextureName2 = dbcIterator.GetString(5);
        TextureName3 = dbcIterator.GetString(6);
        Flags = dbcIterator.GetInt(7);
        VariationIndex = dbcIterator.GetInt(8);
        ColorIndex = dbcIterator.GetInt(9);
    }

    private CharSections()
    {
        Id = 0;
        RaceID = 0;
        SexId = 0;
        BaseSection = 0;
        TextureName1 = "";
        TextureName2 = "";
        TextureName3 = "";
        Flags = 0;
        VariationIndex = 0;
        ColorIndex = 0;
    }

    public static CharSections Empty => new CharSections();
}