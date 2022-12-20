using WDE.Common.DBC;
using WDE.MpqReader.Structures;

namespace WDE.MpqReader.DBC;

public class CharSections
{
    public readonly uint Id;
    public readonly int RaceID;
    public readonly int SexId;
    public readonly int BaseSection;
    public readonly FileId TextureName1;
    public readonly FileId TextureName2;
    public readonly FileId TextureName3;
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

    public CharSections(IWdcIterator dbcIterator, TextureFileDataStore textureFileDataStore)
    {
        Id = (uint)dbcIterator.Id;
        RaceID = dbcIterator.GetByte("RaceID");
        SexId = dbcIterator.GetByte("SexID");
        BaseSection = dbcIterator.GetByte("BaseSection");
        var textureId1 = dbcIterator.GetInt("MaterialResourcesID", 0);
        var textureId2 = dbcIterator.GetInt("MaterialResourcesID", 1);
        var textureId3 = dbcIterator.GetInt("MaterialResourcesID", 2);
        if (textureId1 > 0 && textureFileDataStore.TryGetValue(textureId1, out var textureFileData))
            TextureName1 = textureFileData.FileData;
        if (textureId2 > 0 && textureFileDataStore.TryGetValue(textureId2, out textureFileData))
            TextureName2 = textureFileData.FileData;
        if (textureId3 > 0 && textureFileDataStore.TryGetValue(textureId3, out textureFileData))
            TextureName3 = textureFileData.FileData;
        Flags = dbcIterator.GetShort("Flags");
        VariationIndex = dbcIterator.GetByte("VariationIndex");
        ColorIndex = dbcIterator.GetByte("ColorIndex");
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