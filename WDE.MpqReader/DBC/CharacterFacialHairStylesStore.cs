using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharacterFacialHairStylesStore : BaseDbcStore<uint, CharacterFacialHairStyles>
{
    public CharacterFacialHairStylesStore(IEnumerable<IDbcIterator> rows, GameFilesVersion version)
    {
        uint idx = 1;
        foreach (var row in rows)
        {
            var o = new CharacterFacialHairStyles(row, version);
            store[idx] = o;

            idx++;
        }
    }
    
    public CharacterFacialHairStylesStore(IEnumerable<IWdcIterator> rows, GameFilesVersion version)
    {
        uint idx = 1;
        foreach (var row in rows)
        {
            var o = new CharacterFacialHairStyles(row, version);
            store[idx] = o;

            idx++;
        }
    }
}