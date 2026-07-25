using WDE.Module.Attributes;

namespace WDE.QueryGenerators.Base;

/// <summary>One <c>spawn_group</c> flag bit with its display name (core-specific semantics).</summary>
public record SpawnGroupFlagDefinition(uint Flag, string Name, string? Tooltip = null, bool CreatureOnly = false);

/// <summary>
/// Per-core schema knowledge about the spawn-group tables that isn't expressible through the query
/// providers alone: which flag bits the core understands and what they mean, and which extra
/// columns exist. One implementation per core family ([RequiresCore]), so editor code stays free
/// of core/schema conditionals. Table-level capabilities (formation/random entries/linked
/// groups/squads) are detected via the corresponding IQueryGenerator&lt;T&gt;.TableName instead.
/// </summary>
[NonUniqueProvider]
public interface ISpawnGroupSchemaInfoProvider
{
    /// <summary>The core's spawn_group Flags bits, for checkbox UI.</summary>
    IReadOnlyList<SpawnGroupFlagDefinition> GroupFlags { get; }

    /// <summary>True when a group is strictly creature XOR gameobject (CMaNGOS); false when mixed
    /// membership is allowed.</summary>
    bool GroupsAreStrictlyTyped { get; }

    /// <summary>True when spawn_group has MaxCount/WorldState/WorldStateExpression/StringId/
    /// RespawnOverride columns.</summary>
    bool SupportsFullSpawnGroupRow { get; }

    /// <summary>True when spawn_group_spawn has SlotId (0 = formation leader, -1 = none) and
    /// Chance columns.</summary>
    bool SupportsFormationSlots { get; }
}

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupSchemaInfoProvider : ISpawnGroupSchemaInfoProvider
{
    // SpawnGroupDefines.h: CreatureGroupFlags + SpawnGroupFlags
    private static readonly SpawnGroupFlagDefinition[] flags =
    {
        new(0x01, "Aggro together", "All group members aggro when one does", CreatureOnly: true),
        new(0x02, "Respawn together", "Members respawn as one unit", CreatureOnly: true),
        new(0x04, "Evade together", "All members evade when one does", CreatureOnly: true),
        new(0x08, "Despawn on condition fail", "Despawn the group when its worldstate condition stops being true"),
        new(0x10, "Formation mirroring", "Mirrors slot positions during linear path movement", CreatureOnly: true),
    };

    public IReadOnlyList<SpawnGroupFlagDefinition> GroupFlags => flags;
    public bool GroupsAreStrictlyTyped => true;
    public bool SupportsFullSpawnGroupRow => true;
    public bool SupportsFormationSlots => true;
}
