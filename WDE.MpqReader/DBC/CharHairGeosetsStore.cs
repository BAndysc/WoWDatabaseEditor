using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharHairGeosetsStore : NativeBaseDbcStore<uint, CharHairGeosets>
{
    public CharHairGeosetsStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CharHairGeosets(row, version);
            Set(o.Id, ref o);
        }
    }

    public CharHairGeosetsStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CharHairGeosets(row);
            Set(o.Id, ref o);
        }
    }
}
