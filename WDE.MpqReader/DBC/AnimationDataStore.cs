using System;
using System.Collections.Generic;
using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AnimationDataStore : NativeBaseDbcStore<uint, AnimationData>
{
    public AnimationDataStore(IDBC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AnimationData(row, version);
            Set(o.Id, ref o);
            MaxId = Math.Max(MaxId, o.Id);
        }
    }
    
    public AnimationDataStore(IWDC rows, GameFilesVersion version) : base(rows.RecordCount)
    {
        int index = 0;
        foreach (var row in rows)
        {
            ref var o = ref this.ElementAt(index++);
            o = new AnimationData(row, version);
            Set(o.Id, ref o);
            MaxId = Math.Max(MaxId, o.Id);
        }
    }

    public uint MaxId { get; private set; }
}