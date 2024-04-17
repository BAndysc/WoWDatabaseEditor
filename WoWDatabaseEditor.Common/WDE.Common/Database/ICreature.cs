using WDE.Common.Utils;

namespace WDE.Common.Database
{
    public interface ICreature
    {
        uint Guid { get; }
        uint Entry { get; }
        
        int Map { get; }
        uint? PhaseMask { get; }
        SmallReadOnlyList<int>? PhaseId { get; }
        int? PhaseGroup { get; }
        int EquipmentId { get; }
        uint Model { get; }
        MovementType MovementType { get; }
        float WanderDistance { get; }
        
        float X { get; }
        float Y { get; }
        float Z { get; }
        float O { get; }
        
        uint SpawnKey => 0;
    }

    public enum MovementType
    {
        Idle = 0,
        Random = 1,
        Waypoint = 2,
        SplinePath = 3, // cmangos
        LinearPath = 4 // cmangos
    }

    public interface IBaseCreatureAddon
    {
        uint PathId { get; }
        uint Mount { get; }
        uint MountCreatureId { get; }
        byte StandState { get; }
        byte VisFlags { get; }
        byte AnimTier { get; }
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

    public static class CreatureExtensions
    {
        public static SpawnKey Key(this ICreature that)
        {
            return new SpawnKey(that.Entry, that.Guid);
        }
    }
}