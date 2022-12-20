using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AnimationDataStore : BaseDbcStore<uint, AnimationData>
{
    public AnimationDataStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AnimationData(row, version);
            store[o.Id] = o;
        }
    }
    
    public AnimationDataStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AnimationData(row, version);
            store[o.Id] = o;
        }
    }
}