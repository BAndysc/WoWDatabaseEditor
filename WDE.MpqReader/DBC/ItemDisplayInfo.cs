using WDE.Common.DBC;
using WDE.Common.MPQ;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public ref struct ItemDisplayInfo
{
    public readonly uint Id;
    public readonly FileId LeftModel;
    public readonly FileId RightModel;
    public readonly FileId LeftModelTexture;
    public readonly FileId RightModelTexture;
    public readonly ManagedString Icon1;
    public readonly ManagedString Icon2;
    public readonly int geosetGroup1;
    public readonly int geosetGroup2;
    public readonly int geosetGroup3;
    public readonly int Flags;
    public readonly int SpellVisualID;
    public readonly int groupSoundIndex;
    public readonly int helmetGeosetVisMale;
    public readonly int helmetGeosetVisFemale;
    public readonly ManagedString UpperArmTexture;
    public readonly ManagedString LowerArmTexture;
    public readonly ManagedString HandsTexture;
    public readonly ManagedString UpperTorsoTexture;
    public readonly ManagedString LowerTorsoTexture;
    public readonly ManagedString UpperLegTexture;
    public readonly ManagedString LowerLegTexture;
    public readonly ManagedString FootTexture;
    public readonly ManagedString Texture9;
    public readonly int itemVisual;
    public readonly int particleColorID;


    public ItemDisplayInfo(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetUInt(0);
        LeftModel = dbcIterator.GetString(1);
        RightModel = dbcIterator.GetString(2);
        LeftModelTexture = dbcIterator.GetString(3);
        RightModelTexture = dbcIterator.GetString(4);
        Icon1 = ManagedString.Create(dbcIterator.GetString(5));
        Icon2 = ManagedString.Create(dbcIterator.GetString(6));
        geosetGroup1 = dbcIterator.GetInt(7);
        geosetGroup2 = dbcIterator.GetInt(8);
        geosetGroup3 = dbcIterator.GetInt(9);
        Flags = dbcIterator.GetInt(10);
        SpellVisualID = dbcIterator.GetInt(11);
        groupSoundIndex = dbcIterator.GetInt(12);
        helmetGeosetVisMale = dbcIterator.GetInt(13);
        helmetGeosetVisFemale = dbcIterator.GetInt(14);
        UpperArmTexture = ManagedString.Create(dbcIterator.GetString(15));
        LowerArmTexture = ManagedString.Create(dbcIterator.GetString(16));
        HandsTexture = ManagedString.Create(dbcIterator.GetString(17));
        UpperTorsoTexture = ManagedString.Create(dbcIterator.GetString(18));
        LowerTorsoTexture = ManagedString.Create(dbcIterator.GetString(19));
        UpperLegTexture = ManagedString.Create(dbcIterator.GetString(20));
        LowerLegTexture = ManagedString.Create(dbcIterator.GetString(21));
        FootTexture = ManagedString.Create(dbcIterator.GetString(22));
        if (version == GameFilesVersion.Mop_5_4_8)
        {
            Texture9 = ManagedString.Create(dbcIterator.GetString(23));
            itemVisual = dbcIterator.GetInt(24);
            particleColorID = dbcIterator.GetInt(25);
        }
        else
        {
            itemVisual = dbcIterator.GetInt(23);
            particleColorID = dbcIterator.GetInt(24);
        }
    }
    
    public unsafe ItemDisplayInfo(IWdcIterator dbcIterator, GameFilesVersion version, ModelFileDataStore modelFileDataStore, TextureFileDataStore textureFileDataStore)
    {
        Id = (uint)dbcIterator.Id;
        
        var leftModelId = dbcIterator.GetUInt("ModelResourcesID", 0);
        var rightModelId = dbcIterator.GetUInt("ModelResourcesID", 1);
        var leftModelTextureId = dbcIterator.GetInt("ModelMaterialResourcesID", 0);
        var rightModelTextureId = dbcIterator.GetInt("ModelMaterialResourcesID", 1);

        if (leftModelId > 0 && modelFileDataStore.TryGetValue((int)leftModelId, out var modelData))
            LeftModel = modelData->FileData;
        
        if (rightModelId > 0 && modelFileDataStore.TryGetValue((int)rightModelId, out modelData))
            RightModel = modelData->FileData;
        
        if (leftModelTextureId > 0 && textureFileDataStore.TryGetValue(leftModelTextureId, out var textureFileData))
            LeftModelTexture = textureFileData->FileData;
        
        if (rightModelTextureId > 0 && textureFileDataStore.TryGetValue(rightModelTextureId, out textureFileData))
            RightModelTexture = textureFileData->FileData;
    }
    
    public ItemDisplayInfo()
    {
        Id = 0;
        LeftModel = "";
        RightModel = "";
        LeftModelTexture = "";
        RightModelTexture = "";
        Icon1 = ManagedString.Empty;
        Icon2 = ManagedString.Empty;
        geosetGroup1 = 0;
        geosetGroup2 = 0;
        geosetGroup3 = 0;
        Flags = 0;
        SpellVisualID = 0;
        groupSoundIndex = 0;
        helmetGeosetVisMale = 0;
        helmetGeosetVisFemale = 0;
        UpperArmTexture = ManagedString.Empty;
        LowerArmTexture = ManagedString.Empty;
        HandsTexture = ManagedString.Empty;
        UpperTorsoTexture = ManagedString.Empty;
        LowerTorsoTexture = ManagedString.Empty;
        UpperLegTexture = ManagedString.Empty;
        LowerLegTexture = ManagedString.Empty;
        FootTexture = ManagedString.Empty;
        itemVisual = 0;
        particleColorID = 0;
    }

    public static ItemDisplayInfo Empty => new ItemDisplayInfo();
}