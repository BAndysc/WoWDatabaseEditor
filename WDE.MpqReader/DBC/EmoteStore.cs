using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class EmoteStore : BaseDbcStore<uint, Emote>
{
    public EmoteStore(IEnumerable<IDbcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new Emote(row);
            store[o.Id] = o;
        }
    }
    
    public EmoteStore(IEnumerable<IWdcIterator> rows)
    {
        foreach (var row in rows)
        {
            var o = new Emote(row);
            store[o.Id] = o;
        }
    }
}