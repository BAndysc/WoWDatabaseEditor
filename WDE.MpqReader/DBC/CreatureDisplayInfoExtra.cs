using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoExtra
{
    public readonly uint Id;
    public readonly int Race;
    public readonly int CreatureType;
    public readonly int Gender;
    public readonly int SkinColor;
    public readonly int FaceType;
    public readonly int HairStyle;
    public readonly int HairColor;
    public readonly int BeardStyle;
    public readonly int Helm;
    public readonly int Shoulder;
    public readonly int Shirt;
    public readonly int Cuirass;
    public readonly int Belt;
    public readonly int Legs;
    public readonly int Boots;
    public readonly int Wrist;
    public readonly int Gloves;
    public readonly int Tabard;
    public readonly int Cape;
    public readonly int Flags;
    public readonly string Texture;


    public CreatureDisplayInfoExtra(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        Race = dbcIterator.GetInt(1);
        // CreatureType = dbcIterator.GetInt(2);
        Gender = dbcIterator.GetInt(2);
        SkinColor = dbcIterator.GetInt(3);
        FaceType = dbcIterator.GetInt(4);
        HairStyle = dbcIterator.GetInt(5);
        HairColor = dbcIterator.GetInt(6);
        BeardStyle = dbcIterator.GetInt(7);
        Helm = dbcIterator.GetInt(8);
        Shoulder = dbcIterator.GetInt(9);
        Shirt = dbcIterator.GetInt(10);
        Cuirass = dbcIterator.GetInt(11);
        Belt = dbcIterator.GetInt(12);
        Legs = dbcIterator.GetInt(13);
        Boots = dbcIterator.GetInt(14);
        Wrist = dbcIterator.GetInt(15);
        Gloves = dbcIterator.GetInt(16);
        Tabard = dbcIterator.GetInt(17);
        Cape = dbcIterator.GetInt(18);
        Flags = dbcIterator.GetInt(19);
        Texture = dbcIterator.GetString(20);
    }

    private CreatureDisplayInfoExtra()
    {
        Id = 0;
        Race = 0;
        CreatureType = 0;
        Gender = 0;
        SkinColor = 0;
        FaceType = 0;
        HairStyle = 0;
        HairColor = 0;
        BeardStyle = 0;
        Helm = 0;
        Shoulder = 0;
        Shirt = 0;
        Cuirass = 0;
        Belt = 0;
        Legs = 0;
        Boots = 0;
        Wrist = 0;
        Gloves = 0;
        Tabard = 0;
        Cape = 0;
        Flags = 0;
        Texture = "";
    }

    public static CreatureDisplayInfoExtra Empty => new CreatureDisplayInfoExtra();
}