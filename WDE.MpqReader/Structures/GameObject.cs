using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
{
    public class GameObjectDisplayInfo
    {
        public readonly int Id;
        public readonly string ModelName;
        public readonly int Sound;
        public readonly float GeoBoxMinX;
        public readonly float GeoBoxMinY;
        public readonly float GeoBoxMinZ;
        public readonly float GeoBoxMaxX;
        public readonly float GeoBoxMaxY;
        public readonly float GeoBoxMaxZ;
        public readonly int ObjectEffectPackageID;

        public GameObjectDisplayInfo(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetInt(0);
            ModelName = dbcIterator.GetString(1);
            Sound = dbcIterator.GetInt(2);
            GeoBoxMinX = dbcIterator.GetFloat(3);
            GeoBoxMinY = dbcIterator.GetFloat(4);
            GeoBoxMinZ = dbcIterator.GetFloat(5);
            GeoBoxMaxX = dbcIterator.GetFloat(6);
            GeoBoxMaxY = dbcIterator.GetFloat(7);
            GeoBoxMaxZ = dbcIterator.GetFloat(8);
            ObjectEffectPackageID = dbcIterator.GetInt(9);
        }

        private GameObjectDisplayInfo()
        {
            Id = -1;
            ModelName = "";
            Sound = -1;
            GeoBoxMinX = 0;
            GeoBoxMinY = 0;
            GeoBoxMinZ = 0;
            GeoBoxMaxX = 0;
            GeoBoxMaxY = 0;
            GeoBoxMaxZ = 0;
            ObjectEffectPackageID = 0;

        }

        public static GameObjectDisplayInfo Empty => new GameObjectDisplayInfo();
    }

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

}