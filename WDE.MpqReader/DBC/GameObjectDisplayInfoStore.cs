using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class GameObjectDisplayInfoStore : IEnumerable<GameObjectDisplayInfo>
{
    private Dictionary<int, GameObjectDisplayInfo> store = new();
    public GameObjectDisplayInfoStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new GameObjectDisplayInfo(row);
            store[o.Id] = o;
        }
    }

    public bool Contains(int id) => store.ContainsKey(id);
    public GameObjectDisplayInfo this[int id] => store[id];
    public IEnumerator<GameObjectDisplayInfo> GetEnumerator() => store.Values.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
}