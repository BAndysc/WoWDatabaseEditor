using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class TextureFileDataStore : NativeBaseDbcStore<int, TextureFileData>
{
    public TextureFileDataStore() : base(0)
    {

    }

    public TextureFileDataStore(IWDC dbcIterator) : base(dbcIterator.RecordCount)
    {
        int index = 0;
        foreach (var i in dbcIterator)
        {
            ref var o = ref this.ElementAt(index++);
            o = new TextureFileData(i);
            Set(o.MaterialId, ref o);
        }
    }
}