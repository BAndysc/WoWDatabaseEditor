using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoExtra
{
    public readonly uint Id;
    public readonly uint Race;
    public readonly uint Gender;
    public readonly int SkinColor;
    public readonly int FaceType;
    public readonly int HairStyle;
    public readonly int HairColor;
    public readonly int BeardStyle;
    public readonly uint Helm;
    public readonly uint Shoulder;
    public readonly uint Shirt;
    public readonly uint Cuirass;
    public readonly uint Belt;
    public readonly uint Legs;
    public readonly uint Boots;
    public readonly uint Wrist;
    public readonly uint Gloves;
    public readonly uint Tabard;
    public readonly uint Cape;
    public readonly int Flags;
    public readonly FileId Texture;


    public CreatureDisplayInfoExtra(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        Race = dbcIterator.GetUInt(1);
        // CreatureType = dbcIterator.GetInt(2);
        Gender = dbcIterator.GetUInt(2);
        SkinColor = dbcIterator.GetInt(3);
        FaceType = dbcIterator.GetInt(4);
        HairStyle = dbcIterator.GetInt(5);
        HairColor = dbcIterator.GetInt(6);
        BeardStyle = dbcIterator.GetInt(7);
        Helm = dbcIterator.GetUInt(8);
        Shoulder = dbcIterator.GetUInt(9);
        Shirt = dbcIterator.GetUInt(10);
        Cuirass = dbcIterator.GetUInt(11);
        Belt = dbcIterator.GetUInt(12);
        Legs = dbcIterator.GetUInt(13);
        Boots = dbcIterator.GetUInt(14);
        Wrist = dbcIterator.GetUInt(15);
        Gloves = dbcIterator.GetUInt(16);
        Tabard = dbcIterator.GetUInt(17);
        Cape = dbcIterator.GetUInt(18);
        Flags = dbcIterator.GetInt(19);
        Texture = dbcIterator.GetString(20);
    }

    public CreatureDisplayInfoExtra(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        Race = (uint)dbcIterator.GetSByte("DisplayRaceID");
        // CreatureType = dbcIterator.GetInt(2);
        Gender = (uint)dbcIterator.GetSByte("DisplaySexID");
        SkinColor = dbcIterator.GetByte("SkinID");
        FaceType = dbcIterator.GetByte("FaceID");
        HairStyle = dbcIterator.GetByte("HairStyleID");
        HairColor = dbcIterator.GetByte("HairColorID");
        BeardStyle = dbcIterator.GetByte("FacialHairID");
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
        Texture = dbcIterator.GetInt("BakeMaterialResourcesID");
    }
    
    private CreatureDisplayInfoExtra()
    {
        Id = 0;
        Race = 0;
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