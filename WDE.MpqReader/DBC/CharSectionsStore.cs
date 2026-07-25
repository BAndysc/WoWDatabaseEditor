using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharSectionsStore : NativeBaseDbcStore<uint, CharSections>
{
    public CharSectionsStore(IDBC rows, TextureFileDataStore textureFileDataStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CharSections(row);
            Set(o.Id, ref o);
        }
    }

    public CharSectionsStore(IWDC rows, TextureFileDataStore textureFileDataStore) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CharSections(row, textureFileDataStore);
            Set(o.Id, ref o);
        }
    }
}
