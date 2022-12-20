using System.Collections;
using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class CharSectionsStore : BaseDbcStore<uint, CharSections>
{
    public CharSectionsStore(IEnumerable<IDbcIterator> rows, TextureFileDataStore textureFileDataStore)
    {
        foreach (var row in rows)
        {
            var o = new CharSections(row);
            store[o.Id] = o;
        }
    }
    
    public CharSectionsStore(IEnumerable<IWdcIterator> rows, TextureFileDataStore textureFileDataStore)
    {
        foreach (var row in rows)
        {
            var o = new CharSections(row, textureFileDataStore);
            store[o.Id] = o;
        }
    }
}