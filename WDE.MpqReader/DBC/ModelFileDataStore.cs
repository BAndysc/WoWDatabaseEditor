using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class ModelFileDataStore : NativeBaseDbcStore<int, ModelFileData>
{
    public ModelFileDataStore() : base(0)
    {

    }

    public ModelFileDataStore(IWDC dbcIterator) : base(dbcIterator.RecordCount)
    {
        int index = 0;
        foreach (var i in dbcIterator)
        {
            ref var textureFileData = ref this.ElementAt(index++);
            textureFileData = new ModelFileData(i);
            Set(textureFileData.ModelResourceId, ref textureFileData);
        }
    }
}