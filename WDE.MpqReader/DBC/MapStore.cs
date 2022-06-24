using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC
{
    public class MapStore : BaseDbcStore<int, Map>
    {
        public MapStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new Map(row);
                store[o.Id] = o;
            }
        }
    }
}