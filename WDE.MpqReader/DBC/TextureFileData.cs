using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public readonly struct TextureFileData
{
    public readonly int FileData;
    public readonly int MaterialId;

    public TextureFileData(IWdcIterator dbcIterator)
    {
        FileData = dbcIterator.Id;
        MaterialId = dbcIterator.GetInt("MaterialResourcesID");
    }
}