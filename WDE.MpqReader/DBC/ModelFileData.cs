using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ModelFileData
{
    public readonly int FileData;
    public readonly int ModelResourceId;

    public ModelFileData(IWdcIterator dbcIterator)
    {
        FileData = dbcIterator.Id;
        ModelResourceId = dbcIterator.GetInt("ModelResourcesID");
    }
}