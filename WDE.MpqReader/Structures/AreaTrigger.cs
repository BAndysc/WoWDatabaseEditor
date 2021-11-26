using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.Structures
{
    public class AreaTrigger
    {
        public readonly int Id;
        public readonly int ContinentID;
        public readonly float X;
        public readonly float Y;
        public readonly float Z;
        public readonly float Radius;
        public readonly float Box_Length;
        public readonly float Box_Width;
        public readonly float Box_Height;
        public readonly float Box_Yaw;

        public AreaTrigger(IDbcIterator dbcIterator)
        {
            Id = dbcIterator.GetInt(0);
            ContinentID = dbcIterator.GetInt(1);
            X = dbcIterator.GetFloat(2);
            Y = dbcIterator.GetFloat(3);
            Z = dbcIterator.GetFloat(4);
            Radius = dbcIterator.GetFloat(5);
            Box_Length = dbcIterator.GetFloat(6);
            Box_Width = dbcIterator.GetFloat(7);
            Box_Height = dbcIterator.GetFloat(8);
            Box_Yaw = dbcIterator.GetFloat(9);
        }

        private AreaTrigger()
        {
            Id = -1;
            ContinentID = -1;
            X = 0;
            Y = 0;
            Z = 0;
            Radius = 0;
            Box_Length = 0;
            Box_Width = 0;
            Box_Height = 0;
            Box_Yaw = 0;
        }

        public static AreaTrigger Empty => new AreaTrigger();
    }

    public class AreaTriggerStore : IEnumerable<AreaTrigger>
    {
        private Dictionary<int, AreaTrigger> store = new();
        public AreaTriggerStore(IEnumerable<IDbcIterator> rows)
        {
            foreach (var row in rows)
            {
                var o = new AreaTrigger(row);
                store[o.Id] = o;
            }
        }

        public bool Contains(int id) => store.ContainsKey(id);
        public AreaTrigger this[int id] => store[id];
        public IEnumerator<AreaTrigger> GetEnumerator() => store.Values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => store.Values.GetEnumerator();
    }
}
