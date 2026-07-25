using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC
{
    public class MapStore : NativeBaseDbcStore<int, Map>
    {
        public MapStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
        {
            int index = 0;
            foreach (var row in rows)
            {
                ref var o = ref this.ElementAt(index++);
                o = new Map(row, version, ref allocator);
                Set(o.Id, ref o);
            }
        }

        public MapStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
        {
            int index = 0;
            foreach (var row in rows)
            {
                ref var o = ref this.ElementAt(index++);
                o = new Map(row, version, ref allocator);
                Set(o.Id, ref o);
            }
        }
    }
}