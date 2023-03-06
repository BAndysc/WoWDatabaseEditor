namespace WDE.Common.DBC.Structs;

public interface IArea
{
    uint Id { get; }
    string Name { get; }
    uint Flags1 { get; }
    uint Flags2 { get; }
    uint MapId { get; }
    uint ParentAreaId { get; }
    
    IMap? Map { get; }
    IArea? ParentArea { get; }
}