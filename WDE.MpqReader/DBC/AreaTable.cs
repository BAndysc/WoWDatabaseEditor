using WDE.Common.DBC;
using WDE.Common.MPQ;

namespace WDE.MpqReader.DBC;

public class AreaTable
{
    public readonly uint Id;
    public readonly uint MapId;
    public readonly uint ParentAreaId;
    public readonly uint AreaBit;
    public readonly uint Flags;
    public readonly string Name;

    public AreaTable(IDbcIterator dbcIterator, GameFilesVersion version)
    {
        Id = dbcIterator.GetUInt(0);
        MapId = dbcIterator.GetUInt(1);
        ParentAreaId = dbcIterator.GetUInt(2);
        AreaBit = dbcIterator.GetUInt(3);
        Flags = dbcIterator.GetUInt(4);
        if (version <= GameFilesVersion.Cataclysm_4_3_4)
            Name = dbcIterator.GetString(11);
        else
            Name = dbcIterator.GetString(13);
    }
    
    public AreaTable(IWdcIterator dbcIterator)
    {
        Id = (uint)dbcIterator.Id;
        MapId = dbcIterator.GetUShort("ContinentID");
        ParentAreaId = dbcIterator.GetUShort("ParentAreaID");
        AreaBit = dbcIterator.GetUShort("AreaBit");
        Flags = (uint)dbcIterator.GetInt("Flags", 0);
        Name = dbcIterator.GetString("AreaName_lang");
    }
}