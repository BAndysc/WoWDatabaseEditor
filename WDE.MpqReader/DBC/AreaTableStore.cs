using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AreaTableStore : NativeBaseDbcStore<uint, AreaTable>
{
    public AreaTableStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AreaTable(row, version, ref allocator);
            Set(o.Id, ref o);
        }
    }
    
    public AreaTableStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AreaTable(row, ref allocator);
            Set(o.Id, ref o);
        }
    }
}