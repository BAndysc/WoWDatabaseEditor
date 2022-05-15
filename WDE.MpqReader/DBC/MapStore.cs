using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC
{
    public class MapStore : IEnumerable<Map>
    {
        private Dictionary<int, Map> store = new();
        public MapStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new Map(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(int id) => store.ContainsKey(id);
        public Map this[int id] => store[id];
        public IEnumerator<Map> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }
}