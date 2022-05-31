using WDE.Common.DBC;

namespace WDE.MpqReader.DBC;

public class AreaTable
{
    public readonly uint Id;
    public readonly uint MapId;
    public readonly uint ParentAreaId;
    public readonly uint AreaBit;
    public readonly uint Flags;
    public readonly string Name;

    public AreaTable(IDbcIterator dbcIterator)
    {
        Id = dbcIterator.GetUInt(0);
        MapId = dbcIterator.GetUInt(1);
        ParentAreaId = dbcIterator.GetUInt(2);
        AreaBit = dbcIterator.GetUInt(3);
        Flags = dbcIterator.GetUInt(4);
        Name = dbcIterator.GetString(11);
    }
}