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
            MaxId = Math.Max(MaxId, o.Id);
        }
    }
    
    public AnimationDataStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new AnimationData(row, version);
            store[o.Id] = o;
            MaxId = Math.Max(MaxId, o.Id);
        }
    }

    public uint MaxId { get; private set; }
}