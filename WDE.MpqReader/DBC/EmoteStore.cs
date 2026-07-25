using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class EmoteStore : NativeBaseDbcStore<uint, Emote>
{
    public EmoteStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Emote(row, ref allocator);
            Set(o.Id, ref o);
        }
    }

    public EmoteStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new Emote(row, ref allocator);
            Set(o.Id, ref o);
        }
    }
}
