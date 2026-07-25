using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CreatureDisplayInfoExtraStore : NativeBaseDbcStore<uint, CreatureDisplayInfoExtra>
{
    public CreatureDisplayInfoExtraStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureDisplayInfoExtra(row);
            Set(o.Id, ref o);
        }
    }

    public CreatureDisplayInfoExtraStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new CreatureDisplayInfoExtra(row);
            Set(o.Id, ref o);
        }
    }
}