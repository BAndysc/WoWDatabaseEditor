using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class TextureFileDataStore : BaseDbcStore<int, TextureFileData>
{
    public TextureFileDataStore()
    {
        
    }
    
    public TextureFileDataStore(IEnumerable<IWdcIterator> dbcIterator)
    {
        foreach (var i in dbcIterator)
        {
            var textureFileData = new TextureFileData(i);
            store[textureFileData.MaterialId] = textureFileData;
        }
    }
}