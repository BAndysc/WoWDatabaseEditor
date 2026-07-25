using System.Collections;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class CharacterFacialHairStylesStore : NativeBaseDbcStore<uint, CharacterFacialHairStyles>
{
    public CharacterFacialHairStylesStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int idx = 1;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(idx - 1);
            o = new CharacterFacialHairStyles(row, version);
            Set((uint)idx, ref o);

            idx++;
        }
    }
    
    public CharacterFacialHairStylesStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int idx = 1;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(idx - 1);
            o = new CharacterFacialHairStyles(row, version);
            Set((uint)idx, ref o);

            idx++;
        }
    }
}