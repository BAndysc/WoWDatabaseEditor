using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ItemDisplayInfo
{
    public readonly uint Id;
    public readonly string LeftModel;
    public readonly string RightModel;
    public readonly string LeftModelTexture;
    public readonly string RightModelTexture;
    public readonly string Icon1;
    public readonly string Icon2;
    public readonly int geosetGroup1;
    public readonly int geosetGroup2;
    public readonly int geosetGroup3;
    public readonly int Flags;
    public readonly int SpellVisualID;
    public readonly int groupSoundIndex;
    public readonly int helmetGeosetVisMale;
    public readonly int helmetGeosetVisFemale;
    public readonly string UpperArmTexture;
    public readonly string LowerArmTexture;
    public readonly string HandsTexture;
    public readonly string UpperTorsoTexture;
    public readonly string LowerTorsoTexture;
    public readonly string UpperLegTexture;
    public readonly string LowerLegTexture;
    public readonly string FootTexture;
    public readonly int itemVisual;
    public readonly int particleColorID;


    public ItemDisplayInfo(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        LeftModel = dbcIterator.GetString(1);
        RightModel = dbcIterator.GetString(2);
        LeftModelTexture = dbcIterator.GetString(3);
        RightModelTexture = dbcIterator.GetString(4);
        Icon1 = dbcIterator.GetString(5);
        Icon2 = dbcIterator.GetString(6);
        geosetGroup1 = dbcIterator.GetInt(7);
        geosetGroup2 = dbcIterator.GetInt(8);
        geosetGroup3 = dbcIterator.GetInt(9);
        Flags = dbcIterator.GetInt(10);
        SpellVisualID = dbcIterator.GetInt(11);
        groupSoundIndex = dbcIterator.GetInt(12);
        helmetGeosetVisMale = dbcIterator.GetInt(13);
        helmetGeosetVisFemale = dbcIterator.GetInt(14);
        UpperArmTexture = dbcIterator.GetString(15);
        LowerArmTexture = dbcIterator.GetString(16);
        HandsTexture = dbcIterator.GetString(17);
        UpperTorsoTexture = dbcIterator.GetString(18);
        LowerTorsoTexture = dbcIterator.GetString(19);
        UpperLegTexture = dbcIterator.GetString(20);
        LowerLegTexture = dbcIterator.GetString(21);
        FootTexture = dbcIterator.GetString(22);
        itemVisual = dbcIterator.GetInt(23);
        particleColorID = dbcIterator.GetInt(24);
    }

    private ItemDisplayInfo()
    {
        Id = 0;
        LeftModel = "";
        RightModel = "";
        LeftModelTexture = "";
        RightModelTexture = "";
        Icon1 = "";
        Icon2 = "";
        geosetGroup1 = 0;
        geosetGroup2 = 0;
        geosetGroup3 = 0;
        Flags = 0;
        SpellVisualID = 0;
        groupSoundIndex = 0;
        helmetGeosetVisMale = 0;
        helmetGeosetVisFemale = 0;
        UpperArmTexture = "";
        LowerArmTexture = "";
        HandsTexture = "";
        UpperTorsoTexture = "";
        LowerTorsoTexture = "";
        UpperLegTexture = "";
        LowerLegTexture = "";
        FootTexture = "";
        itemVisual = 0;
        particleColorID = 0;
    }

    public static ItemDisplayInfo Empty => new ItemDisplayInfo();
}