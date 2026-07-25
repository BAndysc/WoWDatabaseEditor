using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class ItemDisplayInfoStore : NativeBaseDbcStore<uint, ItemDisplayInfo>
{
    public ItemDisplayInfoStore(IDBC rows, GameFilesVersion version, ModelFileDataStore modelFileDataStore, TextureFileDataStore textureFileDataStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ItemDisplayInfo(row, version);
            Set(o.Id, ref o);
        }
    }

    public ItemDisplayInfoStore(IWDC rows, GameFilesVersion version, ModelFileDataStore modelFileDataStore, TextureFileDataStore textureFileDataStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new ItemDisplayInfo(row, version, modelFileDataStore, textureFileDataStore);
            Set(o.Id, ref o);
        }
    }
}