namespace WDE.Common.Database;

/// <summary>One CMaNGOS <c>world_safe_locs</c> row: a graveyard / safe-resurrect position, also
/// referenced by id from battlegrounds, instance scripts and teleport spells.</summary>
public interface IWorldSafeLoc
{
    uint Id { get; }
    uint Map { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
    float O { get; }
    string Name { get; }
}

/// <summary>One CMaNGOS <c>game_graveyard_zone</c> row: makes a safe loc usable on death.
/// <see cref="GhostLoc"/> is an area/zone id or a map id depending on <see cref="LinkKind"/>.</summary>
public interface IGraveyardLink
{
    uint SafeLocId { get; }
    uint GhostLoc { get; }
    GraveyardLinkKind LinkKind { get; }
    /// <summary>Team allowed to use the link: 0 = both, 67 = Horde, 469 = Alliance.</summary>
    uint Faction { get; }
}

public enum GraveyardLinkKind
{
    Area = 0,
    Map = 1,
}

/// <summary>One CMaNGOS <c>spell_target_position</c> row: the destination of a spell with the
/// TARGET_LOCATION_DATABASE (17) implicit target. Keyed by spell id - one destination per spell.</summary>
public interface ISpellTargetPosition
{
    uint SpellId { get; }
    uint Map { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
    float O { get; }
}

public class AbstractWorldSafeLoc : IWorldSafeLoc
{
    public uint Id { get; set; }
    public uint Map { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
    public string Name { get; set; } = "";
}

public class AbstractGraveyardLink : IGraveyardLink
{
    public uint SafeLocId { get; set; }
    public uint GhostLoc { get; set; }
    public GraveyardLinkKind LinkKind { get; set; }
    public uint Faction { get; set; }
}

public class AbstractSpellTargetPosition : ISpellTargetPosition
{
    public uint SpellId { get; set; }
    public uint Map { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
}
