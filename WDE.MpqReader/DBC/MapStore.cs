using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC
{
    public class MapStore : BaseDbcStore<int, Map>
    {
        public MapStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
        {
            foreach (var row in rows)
            {
                var o = new Map(row, version);
                store[o.Id] = o;
            }
        }
        
        public MapStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
        {
            foreach (var row in rows)
            {
                var o = new Map(row, version);
                store[o.Id] = o;
            }
        }
    }
}