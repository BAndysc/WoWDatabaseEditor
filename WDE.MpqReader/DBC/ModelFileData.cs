using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public ref struct ModelFileData
{
    public readonly int FileData;
    public readonly int ModelResourceId;

    public ModelFileData(IWdcIterator dbcIterator)
    {
        FileData = dbcIterator.Id;
        ModelResourceId = dbcIterator.GetInt("ModelResourcesID");
    }
}