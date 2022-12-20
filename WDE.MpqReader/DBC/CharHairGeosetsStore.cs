using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharHairGeosetsStore : BaseDbcStore<uint, CharHairGeosets>
{
    public CharHairGeosetsStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new CharHairGeosets(row, version);
            store[o.Id] = o;
        }
    }

    public CharHairGeosetsStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        foreach (var row in rows)
        {
            var o = new CharHairGeosets(row);
            store[o.Id] = o;
        }
    }
}