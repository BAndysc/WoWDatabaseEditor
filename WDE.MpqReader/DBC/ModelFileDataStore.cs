using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ModelFileDataStore : BaseDbcStore<int, ModelFileData>
{
    public ModelFileDataStore()
    {
        
    }
    
    public ModelFileDataStore(IEnumerable<IWdcIterator> dbcIterator)
    {
        foreach (var i in dbcIterator)
        {
            var textureFileData = new ModelFileData(i);
            store[textureFileData.ModelResourceId] = textureFileData;
        }
    }
}