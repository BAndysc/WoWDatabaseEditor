using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class LiquidObjectStore : NativeBaseDbcStore<int, LiquidObject>
{
    public LiquidObjectStore() : base(0) {}

    public LiquidObjectStore(IDBC dbcIterator) : base(dbcIterator.RecordCount)
    {
        int index = 0;
        foreach (var iterator in dbcIterator)
        {
            ref var liquidObject = ref this.ElementAt(index++);
            liquidObject = new LiquidObject(iterator);
            Set(liquidObject.Id, ref liquidObject);
        }
    }

    public LiquidObjectStore(IWDC dbcIterator) : base(dbcIterator.RecordCount)
    {
        int index = 0;
        foreach (var iterator in dbcIterator)
        {
            ref var liquidObject = ref this.ElementAt(index++);
            liquidObject = new LiquidObject(iterator);
            Set(liquidObject.Id, ref liquidObject);
        }
    }
}
