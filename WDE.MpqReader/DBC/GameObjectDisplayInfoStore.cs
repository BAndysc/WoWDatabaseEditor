using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class GameObjectDisplayInfoStore : NativeBaseDbcStore<uint, GameObjectDisplayInfo>
{
    public GameObjectDisplayInfoStore(IDBC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new GameObjectDisplayInfo(row);
            Set(o.Id, ref o);
        }
    }

    public GameObjectDisplayInfoStore(IWDC rows) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new GameObjectDisplayInfo(row);
            Set(o.Id, ref o);
        }
    }
}
