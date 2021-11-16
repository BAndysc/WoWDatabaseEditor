using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
{
    public class Map
    {
        public readonly int Id;
        public readonly string Directory;
        public readonly string Name;

        public Map(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetInt(0);
            Directory = dbcIterator.GetString(1);
            Name = dbcIterator.GetString(5);
        }

        private Map()
        {
            Id = -1;
            Directory = "";
            Name = "(null)";
        }

        public static Map Empty => new Map();
    }

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