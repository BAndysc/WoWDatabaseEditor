using System.Collections.Generic;

namespace WDE.Common.Database;

/// <summary>The full CMaNGOS <c>spawn_group</c> row. The base <see cref="ISpawnGroupTemplate"/>
/// covers what all cores share (id/name/type/flags); these columns exist on CMaNGOS only.</summary>
public interface ISpawnGroupTemplateAdvanced : ISpawnGroupTemplate
{
    /// <summary>Maximum active alive entities spawned in the world (0 = all).</summary>
    int MaxCount { get; }
    /// <summary>Worldstate condition id gating spawning (0 = none).</summary>
    int WorldState { get; }
    /// <summary>Worldstate expression id (exclusive with <see cref="WorldState"/>).</summary>
    int WorldStateExpression { get; }
    uint StringId { get; }
    uint? RespawnOverrideMin { get; }
    uint? RespawnOverrideMax { get; }
}

/// <summary>One CMaNGOS <c>spawn_group_entry</c> row: the random-entry pool a member spawn with
/// entry 0 rolls from.</summary>
public interface ISpawnGroupRandomEntry
{
    uint GroupId { get; }
    uint Entry { get; }
    /// <summary>Minimum count of this entry in the group before randomization kicks in.</summary>
    uint MinCount { get; }
    /// <summary>Maximum alive count of this entry (0 = infinite).</summary>
    uint MaxCount { get; }
    uint Chance { get; }
}

/// <summary>One CMaNGOS <c>spawn_group_linked_group</c> row: group -> linked group.</summary>
public interface ISpawnGroupLinkedGroup
{
    uint GroupId { get; }
    uint LinkedGroupId { get; }
}

/// <summary>One CMaNGOS <c>spawn_group_squad</c> row: within a group, squads force specific
/// entries onto specific guids together (used with the random-entry pool).</summary>
public interface ISpawnGroupSquadMember
{
    uint GroupId { get; }
    uint SquadId { get; }
    uint Guid { get; }
    uint Entry { get; }
}

public class AbstractSpawnGroupTemplateAdvanced : AbstractSpawnGroupTemplate, ISpawnGroupTemplateAdvanced
{
    public int MaxCount { get; set; }
    public int WorldState { get; set; }
    public int WorldStateExpression { get; set; }
    public uint StringId { get; set; }
    public uint? RespawnOverrideMin { get; set; }
    public uint? RespawnOverrideMax { get; set; }
}

public class AbstractSpawnGroupRandomEntry : ISpawnGroupRandomEntry
{
    public uint GroupId { get; set; }
    public uint Entry { get; set; }
    public uint MinCount { get; set; }
    public uint MaxCount { get; set; }
    public uint Chance { get; set; }
}

public class AbstractSpawnGroupLinkedGroup : ISpawnGroupLinkedGroup
{
    public uint GroupId { get; set; }
    public uint LinkedGroupId { get; set; }
}

public class AbstractSpawnGroupSquadMember : ISpawnGroupSquadMember
{
    public uint GroupId { get; set; }
    public uint SquadId { get; set; }
    public uint Guid { get; set; }
    public uint Entry { get; set; }
}

public class AbstractSpawnGroupFormation : ISpawnGroupFormation
{
    public uint Id { get; set; }
    public FormationShape FormationType { get; set; }
    public float Spread { get; set; }
    public int Options { get; set; }
    public int PathId { get; set; }
    public MovementType MovementType { get; set; }
    public string? Comment { get; set; }
}
