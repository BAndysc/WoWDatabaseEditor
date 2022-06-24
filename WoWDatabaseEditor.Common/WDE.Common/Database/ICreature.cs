namespace WDE.Common.Database
{
    public interface ICreature
    {
        uint Guid { get; }
        uint Entry { get; }
        
        uint Map { get; }
        uint? PhaseMask { get; }
        int? PhaseId { get; }
        int? PhaseGroup { get; }
        int EquipmentId { get; }
        
        float X { get; }
        float Y { get; }
        float Z { get; }
        float O { get; }
        
        uint SpawnKey => 0;
    }

    public interface IBaseCreatureAddon
    {
        uint PathId { get; }
        uint Mount { get; }
        uint MountCreatureId { get; }
        uint Bytes1 { get; }
        byte Sheath { get; }
        byte PvP { get; }
        uint Emote { get; }
        uint VisibilityDistanceType { get; }
        string? Auras { get; }
    }

    public interface ICreatureAddon : IBaseCreatureAddon
    {
        uint Guid { get; }
    }

    public interface ICreatureTemplateAddon : IBaseCreatureAddon
    {
        uint Entry { get; }
    }
}