using WDE.Common.DBC.Structs;

namespace WDE.DbcStore.Structs;

public class AreaEntry : IArea
{
    public uint Id { get; init; }
    public string Name { get; init; } = "";
    public uint Flags1 { get; init; }
    public uint Flags2 { get; init; }
    public uint MapId { get; init; }
    public uint ParentAreaId { get; init; }
    public IMap? Map { get; set; }
    public IArea? ParentArea { get; set; }
}