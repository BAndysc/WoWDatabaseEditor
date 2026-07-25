using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AreaTriggerStore : NativeBaseDbcStore<int, AreaTrigger>
{
    public AreaTriggerStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AreaTrigger(row, version);
            Set(o.Id, ref o);
        }
    }
    
    public AreaTriggerStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AreaTrigger(row);
            Set(o.Id, ref o);
        }
    }
}