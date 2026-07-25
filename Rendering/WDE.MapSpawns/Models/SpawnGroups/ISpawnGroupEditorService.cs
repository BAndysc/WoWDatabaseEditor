using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawns.Models.SpawnGroups;

public readonly record struct SpawnGroupMember(bool IsCreature, uint Guid);

[UniqueProvider]
public interface ISpawnGroupEditorService
{
    /// <summary>True when the active core has spawn_group SQL providers (gates the tool).</summary>
    bool IsSupported { get; }

    // --- advanced (CMaNGOS) capabilities - each gates a section of the group editor -------------

    /// <summary>True when the core has the extended spawn_group schema (full row + slots) and
    /// per-group <see cref="GetDetails"/> is available.</summary>
    bool SupportsAdvancedEditing { get; }
    /// <summary>spawn_group_formation exists (7 predefined shapes, spread, path movement).</summary>
    bool SupportsFormations { get; }
    /// <summary>spawn_group_entry exists (random-entry pool for entry-0 spawns).</summary>
    bool SupportsRandomEntries { get; }
    /// <summary>spawn_group_linked_group exists.</summary>
    bool SupportsLinkedGroups { get; }
    /// <summary>spawn_group_squad exists.</summary>
    bool SupportsSquads { get; }
    /// <summary>The core's spawn_group Flags bits (empty when the core has no flags column).</summary>
    IReadOnlyList<SpawnGroupFlagDefinition> GroupFlags { get; }
    /// <summary>True when a group is strictly creature XOR gameobject (CMaNGOS).</summary>
    bool GroupsAreStrictlyTyped { get; }

    /// <summary>The full editable state of the group, or null when the core has no advanced schema
    /// (or the group is unknown). Mutate it directly, then call <see cref="NotifyDetailsChanged"/>.</summary>
    SpawnGroupDetails? GetDetails(uint groupId);

    /// <summary>Marks the group dirty after its details were mutated and bumps <see cref="Revision"/>.</summary>
    void NotifyDetailsChanged(uint groupId);

    int LoadedMap { get; }
    bool AnyDirty { get; }

    /// <summary>True once membership (DB + in-memory edits) has been loaded at least once.</summary>
    bool HasData { get; }

    /// <summary>Bumped on every state change (including detail edits like flags/slots) -
    /// observers use it to invalidate caches.</summary>
    int Revision { get; }

    /// <summary>Bumped only when the grouping the spawns tree shows changes (membership, group
    /// names, group create/delete) - detail edits like flags do NOT bump it, so the tree doesn't
    /// rebuild while you edit a group.</summary>
    int StructureRevision { get; }

    /// <summary>Group id -> display name, for the "add to group" picker.</summary>
    IReadOnlyDictionary<uint, string> GroupNames { get; }

    /// <summary>Loads all spawn-group templates and assignments (membership is by guid, all maps).</summary>
    Task LoadForMap(int mapId);

    /// <summary>Merges an off-thread load into the live state. Call once per frame on the engine thread.</summary>
    void PumpPendingLoads();

    uint? GroupOf(SpawnGroupMember member);

    /// <summary>Set by the inspector's "Edit" button to ask the spawn-group tool to open a group
    /// for editing. The tool consumes it once (assigns its selected group, then clears this back to
    /// null). Null = no pending request.</summary>
    uint? RequestedEditGroup { get; set; }

    /// <summary>Fills <paramref name="output"/> (cleared first) with the group's current members.
    /// Allocation-free for callers that reuse the list - use this on hot paths.</summary>
    void CollectMembers(uint templateId, List<SpawnGroupMember> output);

    /// <summary>Creates a new group (auto id, given name) from the members. Returns the new id.</summary>
    uint CreateGroup(string name, IReadOnlyList<SpawnGroupMember> members);

    void AddToGroup(uint templateId, IReadOnlyList<SpawnGroupMember> members);
    void RemoveMember(uint templateId, SpawnGroupMember member);

    /// <summary>Deletes the whole group - the template and every side-table row (membership,
    /// formation, random entries, links from AND to it, squads). Member spawns stay in the world,
    /// just ungrouped. The database rows go away on the next <see cref="Save"/>; deleting a
    /// never-saved group simply forgets it.</summary>
    void DeleteGroup(uint groupId);

    /// <summary>The exact SQL <see cref="Save"/> would execute right now (null = nothing dirty).
    /// Pure: no execution, no state change.</summary>
    WDE.SqlQueryGenerator.IQuery? BuildSaveQuery();

    /// <summary>Writes all dirty groups to the DB (template insert for new groups, member
    /// DELETE+INSERT, all-table deletes for deleted groups).</summary>
    Task Save();
}
