using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AnimationDataStore : BaseDbcStore<uint, AnimationData>
{
    public AnimationDataStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new AnimationData(row);
            store[o.Id] = o;
        }
    }
}