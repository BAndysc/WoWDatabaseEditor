using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoStore : NativeBaseDbcStore<uint, CreatureDisplayInfo>
{
    public CreatureDisplayInfoStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureDisplayInfo(row, version);
            Set(o.Id, ref o);
        }
    }

    public CreatureDisplayInfoStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureDisplayInfo(row, version);
            Set(o.Id, ref o);
        }
    }
}