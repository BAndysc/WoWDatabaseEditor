using System.Collections.Generic;
using WDE.Common.Database;

namespace WDE.MapSpawns.Models.SpawnGroups;

/// <summary>
/// The full editable state of one spawn group beyond bare membership - everything the CMaNGOS
/// spawn-group tables can express. Loaded per group on advanced cores
/// (<see cref="ISpawnGroupEditorService.SupportsAdvancedEditing"/>); the UI mutates it directly
/// and calls <see cref="ISpawnGroupEditorService.NotifyDetailsChanged"/> so the group is marked
/// dirty and observers refresh. Sub-features are individually capability-gated - a core might
/// have slots but no squads.
/// </summary>
public sealed class SpawnGroupDetails
{
    public uint Id { get; init; }

    // spawn_group row
    public string Name = "";
    public SpawnGroupTemplateType Type;
    public uint Flags;
    public int MaxCount;
    public int WorldState;
    public int WorldStateExpression;
    public uint StringId;
    public uint? RespawnOverrideMin;
    public uint? RespawnOverrideMax;

    /// <summary>spawn_group_formation row; null = the group has no formation.</summary>
    public SpawnGroupFormationData? Formation;

    /// <summary>Per-member slot/chance (spawn_group_spawn extra columns), keyed by guid.
    /// Slot 0 = formation leader, -1 = not part of the formation.</summary>
    public readonly Dictionary<uint, SpawnGroupMemberSlot> MemberSlots = new();

    /// <summary>spawn_group_entry rows: the random-entry pool members with entry 0 roll from.</summary>
    public readonly List<SpawnGroupRandomEntryRow> RandomEntries = new();

    /// <summary>spawn_group_linked_group rows: ids of groups linked to this one.</summary>
    public readonly List<uint> LinkedGroups = new();

    /// <summary>spawn_group_squad rows: squads force specific entries onto specific guids together.</summary>
    public readonly List<SpawnGroupSquadRow> Squads = new();

    public SpawnGroupMemberSlot SlotOf(uint guid) =>
        MemberSlots.TryGetValue(guid, out var s) ? s : new SpawnGroupMemberSlot { SlotId = -1 };
}

public struct SpawnGroupMemberSlot
{
    public int SlotId;   // 0 = leader, -1 = not in the formation
    public uint Chance;  // 0 = always spawns
}

public sealed class SpawnGroupFormationData
{
    public FormationShape Shape;
    public float Spread = 3f;         // core clamps to -15..15
    public int Options;               // SpawnGroupFormationOptions bits (0x02 = keep compact)
    public int PathId;                // waypoint_path id when MovementType is a path type
    public MovementType MovementType; // same values as the creature table
    public string? Comment;
}

public struct SpawnGroupRandomEntryRow
{
    public uint Entry;
    public uint MinCount;  // minimum of this entry before randomization
    public uint MaxCount;  // maximum alive of this entry, 0 = infinite
    public uint Chance;
}

public struct SpawnGroupSquadRow
{
    public uint SquadId;
    public uint Guid;
    public uint Entry;
}
