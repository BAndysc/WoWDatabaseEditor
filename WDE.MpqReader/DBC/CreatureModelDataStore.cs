using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureModelDataStore : NativeBaseDbcStore<uint, CreatureModelData>
{
    public CreatureModelDataStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureModelData(row);
            Set(o.Id, ref o);
        }
    }

    public CreatureModelDataStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureModelData(row);
            Set(o.Id, ref o);
        }
    }
}