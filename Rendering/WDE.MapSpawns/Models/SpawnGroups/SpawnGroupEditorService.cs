using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.SpawnGroups;

public class SpawnGroupEditorService : ISpawnGroupEditorService
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<ISpawnGroupTemplate> templateGen;
    private readonly IQueryGenerator<ISpawnGroupSpawn> spawnGen;
    private readonly IQueryGenerator<ISpawnGroupFormation> formationGen;
    private readonly IQueryGenerator<ISpawnGroupRandomEntry> randomEntryGen;
    private readonly IQueryGenerator<ISpawnGroupLinkedGroup> linkedGroupGen;
    private readonly IQueryGenerator<ISpawnGroupSquadMember> squadGen;
    private readonly ISpawnGroupSchemaInfoProvider? schemaInfo;

    private sealed class Loaded
    {
        public required int MapId;
        public required Dictionary<uint, string> Names;
        public required Dictionary<uint, HashSet<SpawnGroupMember>> Members;
        public required Dictionary<uint, SpawnGroupDetails> Details;
    }

    // swapped in on the engine thread by PumpPendingLoads
    private volatile Loaded? pending;

    private readonly Dictionary<uint, string> names = new();
    private readonly Dictionary<uint, HashSet<SpawnGroupMember>> original = new();
    private readonly Dictionary<uint, HashSet<SpawnGroupMember>> current = new();
    private readonly Dictionary<SpawnGroupMember, uint> memberToGroup = new();
    private readonly Dictionary<uint, SpawnGroupDetails> details = new();
    private readonly HashSet<uint> dirty = new();
    private readonly HashSet<uint> newGroups = new();
    private readonly HashSet<uint> deletedGroups = new();

    public uint? RequestedEditGroup { get; set; }
    public bool MemberPickArmed { get; set; }
    public int LoadedMap { get; private set; } = -1;
    public bool AnyDirty => dirty.Count > 0;
    public bool HasData { get; private set; }
    public int Revision { get; private set; }
    public int StructureRevision { get; private set; }
    public IReadOnlyDictionary<uint, string> GroupNames => names;
    public bool IsSupported => spawnGen.TableName != null && templateGen.TableName != null;

    public bool SupportsAdvancedEditing => schemaInfo is { SupportsFullSpawnGroupRow: true };
    public bool SupportsFormations => formationGen.TableName != null;
    public bool SupportsRandomEntries => randomEntryGen.TableName != null;
    public bool SupportsLinkedGroups => linkedGroupGen.TableName != null;
    public bool SupportsSquads => squadGen.TableName != null;
    public IReadOnlyList<SpawnGroupFlagDefinition> GroupFlags =>
        schemaInfo?.GroupFlags ?? Array.Empty<SpawnGroupFlagDefinition>();
    public bool GroupsAreStrictlyTyped => schemaInfo?.GroupsAreStrictlyTyped ?? false;

    public SpawnGroupEditorService(IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<ISpawnGroupTemplate> templateGen,
        IQueryGenerator<ISpawnGroupSpawn> spawnGen,
        IQueryGenerator<ISpawnGroupFormation> formationGen,
        IQueryGenerator<ISpawnGroupRandomEntry> randomEntryGen,
        IQueryGenerator<ISpawnGroupLinkedGroup> linkedGroupGen,
        IQueryGenerator<ISpawnGroupSquadMember> squadGen,
        IEnumerable<ISpawnGroupSchemaInfoProvider> schemaInfoProviders)
    {
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.templateGen = templateGen;
        this.spawnGen = spawnGen;
        this.formationGen = formationGen;
        this.randomEntryGen = randomEntryGen;
        this.linkedGroupGen = linkedGroupGen;
        this.squadGen = squadGen;
        schemaInfo = schemaInfoProviders.FirstOrDefault(); // at most one per core ([RequiresCore])
    }

    public async Task LoadForMap(int mapId)
    {
        // claim the map BEFORE the first await - the module's per-frame SyncMap compares against
        // LoadedMap, and the getters can stall behind the app cache warm-up; without this a new
        // full load is queued every frame until the first one lands (see PoolEditorService)
        LoadedMap = mapId;

        var templates = (await databaseProvider.GetSpawnGroupTemplatesAsync())
            .GroupBy(x => x.Id).ToDictionary(x => x.Key, x => x.First());
        var creatureGroupIds = templates.Values
            .Where(t => t.Type == SpawnGroupTemplateType.Creature)
            .Select(t => t.Id).ToHashSet();

        var spawns = await databaseProvider.GetSpawnGroupSpawnsAsync();
        var members = new Dictionary<uint, HashSet<SpawnGroupMember>>();
        foreach (var gs in spawns)
        {
            var type = gs.Type == SpawnGroupTemplateType.Any
                ? (creatureGroupIds.Contains(gs.TemplateId) ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject)
                : gs.Type;
            if (!members.TryGetValue(gs.TemplateId, out var set))
                members[gs.TemplateId] = set = new HashSet<SpawnGroupMember>();
            set.Add(new SpawnGroupMember(type == SpawnGroupTemplateType.Creature, gs.Guid));
        }

        var detailsMap = new Dictionary<uint, SpawnGroupDetails>();
        if (SupportsAdvancedEditing)
        {
            foreach (var t in templates.Values)
                detailsMap[t.Id] = BuildDetails(t);

            foreach (var gs in spawns)
            {
                if (detailsMap.TryGetValue(gs.TemplateId, out var d))
                    d.MemberSlots[gs.Guid] = new SpawnGroupMemberSlot { SlotId = gs.SlotId ?? -1, Chance = gs.Chance ?? 0 };
            }

            if (SupportsFormations && await databaseProvider.GetSpawnGroupFormations() is { } formations)
            {
                foreach (var f in formations)
                {
                    if (detailsMap.TryGetValue(f.Id, out var d))
                        d.Formation = new SpawnGroupFormationData
                        {
                            Shape = f.FormationType,
                            Spread = f.Spread,
                            Options = f.Options,
                            PathId = f.PathId,
                            MovementType = f.MovementType,
                            Comment = f.Comment,
                        };
                }
            }

            if (SupportsRandomEntries && await databaseProvider.GetSpawnGroupRandomEntriesAsync() is { } entries)
            {
                foreach (var e in entries)
                {
                    if (detailsMap.TryGetValue(e.GroupId, out var d))
                        d.RandomEntries.Add(new SpawnGroupRandomEntryRow
                            { Entry = e.Entry, MinCount = e.MinCount, MaxCount = e.MaxCount, Chance = e.Chance });
                }
            }

            if (SupportsLinkedGroups && await databaseProvider.GetSpawnGroupLinkedGroupsAsync() is { } links)
            {
                foreach (var l in links)
                {
                    if (detailsMap.TryGetValue(l.GroupId, out var d))
                        d.LinkedGroups.Add(l.LinkedGroupId);
                }
            }

            if (SupportsSquads && await databaseProvider.GetSpawnGroupSquadsAsync() is { } squads)
            {
                foreach (var s in squads)
                {
                    if (detailsMap.TryGetValue(s.GroupId, out var d))
                        d.Squads.Add(new SpawnGroupSquadRow { SquadId = s.SquadId, Guid = s.Guid, Entry = s.Entry });
                }
            }
        }

        pending = new Loaded
        {
            MapId = mapId,
            Names = templates.ToDictionary(x => x.Key, x => x.Value.Name),
            Members = members,
            Details = detailsMap,
        };
    }

    private static SpawnGroupDetails BuildDetails(ISpawnGroupTemplate t)
    {
        var adv = t as ISpawnGroupTemplateAdvanced;
        return new SpawnGroupDetails
        {
            Id = t.Id,
            Name = t.Name,
            Type = t.Type,
            Flags = t.MangosFlags ?? t.TrinityFlags ?? 0,
            MaxCount = adv?.MaxCount ?? 0,
            WorldState = adv?.WorldState ?? 0,
            WorldStateExpression = adv?.WorldStateExpression ?? 0,
            StringId = adv?.StringId ?? 0,
            RespawnOverrideMin = adv?.RespawnOverrideMin,
            RespawnOverrideMax = adv?.RespawnOverrideMax,
        };
    }

    public void PumpPendingLoads()
    {
        var p = pending;
        if (p == null)
            return;
        pending = null;

        names.Clear();
        original.Clear();
        current.Clear();
        memberToGroup.Clear();
        details.Clear();
        dirty.Clear();
        newGroups.Clear();
        deletedGroups.Clear();

        foreach (var (id, name) in p.Names)
            names[id] = name;
        foreach (var (id, set) in p.Members)
        {
            original[id] = new HashSet<SpawnGroupMember>(set);
            current[id] = new HashSet<SpawnGroupMember>(set);
            foreach (var m in set)
                memberToGroup[m] = id;
        }
        foreach (var (id, d) in p.Details)
            details[id] = d;
        LoadedMap = p.MapId;
        HasData = true;
        Revision++;
        StructureRevision++;
    }

    public uint? GroupOf(SpawnGroupMember member) =>
        memberToGroup.TryGetValue(member, out var id) ? id : null;

    public void CollectMembers(uint templateId, List<SpawnGroupMember> output)
    {
        output.Clear();
        if (current.TryGetValue(templateId, out var set))
            output.AddRange(set); // AddRange over a HashSet (ICollection) uses CopyTo - no per-item alloc
    }

    public SpawnGroupDetails? GetDetails(uint groupId) =>
        details.TryGetValue(groupId, out var d) ? d : null;

    public void NotifyDetailsChanged(uint groupId)
    {
        if (!details.TryGetValue(groupId, out var d))
            return;
        if (string.IsNullOrWhiteSpace(d.Name))
            d.Name = $"Group {groupId}";
        // structure bump only when the tree-visible name changed - flag/slot edits must not
        // trigger a spawns-tree regroup
        if (!names.TryGetValue(groupId, out var oldName) || oldName != d.Name)
            StructureRevision++;
        names[groupId] = d.Name; // keep the tree/picker name in sync
        dirty.Add(groupId);
        Revision++;
    }

    public uint CreateGroup(string name, IReadOnlyList<SpawnGroupMember> members)
    {
        uint id = NextFreeId();
        names[id] = string.IsNullOrWhiteSpace(name) ? $"Group {id}" : name;
        current[id] = new HashSet<SpawnGroupMember>();
        newGroups.Add(id);
        if (SupportsAdvancedEditing)
            details[id] = new SpawnGroupDetails
            {
                Id = id,
                Name = names[id],
                Type = TypeOf(members) == SpawnGroupTemplateType.GameObject
                    ? SpawnGroupTemplateType.GameObject
                    : SpawnGroupTemplateType.Creature,
            };
        AddToGroup(id, members);
        dirty.Add(id); // even if all members were rejected, the template row itself is new
        return id;
    }

    public void AddToGroup(uint templateId, IReadOnlyList<SpawnGroupMember> members)
    {
        if (!current.TryGetValue(templateId, out var set))
            current[templateId] = set = new HashSet<SpawnGroupMember>();

        var d = GetDetails(templateId);

        foreach (var m in members)
        {
            // CMaNGOS groups are strictly typed - never mix creatures and gameobjects
            if (GroupsAreStrictlyTyped && d != null &&
                m.IsCreature != (d.Type == SpawnGroupTemplateType.Creature))
                continue;

            // a spawn belongs to exactly one group - detach from a previous one
            if (memberToGroup.TryGetValue(m, out var old) && old != templateId)
            {
                if (current.TryGetValue(old, out var oldSet) && oldSet.Remove(m))
                {
                    GetDetails(old)?.MemberSlots.Remove(m.Guid);
                    dirty.Add(old);
                }
            }
            if (set.Add(m))
            {
                dirty.Add(templateId);
                // slot-capable cores: a new member joins the formation right away - the first
                // one leads (slot 0), later ones append after the highest used slot
                if (d != null && m.IsCreature && SupportsFormations &&
                    d.Type == SpawnGroupTemplateType.Creature && !d.MemberSlots.ContainsKey(m.Guid))
                {
                    int next = 0;
                    foreach (var slot in d.MemberSlots.Values)
                        if (slot.SlotId >= 0)
                            next = Math.Max(next, slot.SlotId + 1);
                    d.MemberSlots[m.Guid] = new SpawnGroupMemberSlot { SlotId = next };
                }
            }
            memberToGroup[m] = templateId;
        }
        Revision++;
        StructureRevision++;
    }

    public void RemoveMember(uint templateId, SpawnGroupMember member)
    {
        if (current.TryGetValue(templateId, out var set) && set.Remove(member))
        {
            GetDetails(templateId)?.MemberSlots.Remove(member.Guid);
            dirty.Add(templateId);
            if (memberToGroup.TryGetValue(member, out var g) && g == templateId)
                memberToGroup.Remove(member);
            Revision++;
            StructureRevision++;
        }
    }

    public void DeleteGroup(uint groupId)
    {
        if (!names.Remove(groupId))
            return;

        if (current.TryGetValue(groupId, out var set))
        {
            foreach (var m in set)
            {
                if (memberToGroup.TryGetValue(m, out var g) && g == groupId)
                    memberToGroup.Remove(m);
            }
            current.Remove(groupId);
        }
        details.Remove(groupId);

        // other groups may link to this one - drop those references too, so their rewrite
        // removes the now-dangling spawn_group_linked_group rows
        foreach (var (otherId, d) in details)
        {
            if (d.LinkedGroups.Remove(groupId))
                dirty.Add(otherId);
        }

        if (newGroups.Remove(groupId))
            dirty.Remove(groupId); // never saved - nothing to delete in the database
        else
        {
            deletedGroups.Add(groupId);
            dirty.Add(groupId);
        }

        Revision++;
        StructureRevision++;
    }

    private uint NextFreeId()
    {
        uint max = 0;
        foreach (var id in names.Keys)
            max = Math.Max(max, id);
        // ids deleted this session stay reserved until Save applies the deletes - handing one out
        // again would let a later unsaved delete of the new group forget the pending DB delete
        foreach (var id in deletedGroups)
            max = Math.Max(max, id);
        return max + 1;
    }

    private static SpawnGroupTemplateType TypeOf(IEnumerable<SpawnGroupMember> members)
    {
        bool anyCreature = false, anyGo = false;
        foreach (var m in members)
        {
            anyCreature |= m.IsCreature;
            anyGo |= !m.IsCreature;
        }
        if (anyCreature && anyGo)
            return SpawnGroupTemplateType.Any;
        return anyGo ? SpawnGroupTemplateType.GameObject : SpawnGroupTemplateType.Creature;
    }

    private ISpawnGroupSpawn Row(uint groupId, SpawnGroupMember m)
    {
        var slot = GetDetails(groupId)?.SlotOf(m.Guid);
        return new AbstractSpawnGroupSpawn
        {
            TemplateId = groupId,
            Guid = m.Guid,
            Type = m.IsCreature ? SpawnGroupTemplateType.Creature : SpawnGroupTemplateType.GameObject,
            SlotId = slot?.SlotId ?? -1,
            Chance = slot?.Chance ?? 0,
        };
    }

    /// <summary>Key-only row for <c>TryDeleteAll</c> (deletes the group's whole membership).</summary>
    private static ISpawnGroupSpawn GroupKeyRow(uint groupId) => new AbstractSpawnGroupSpawn
    {
        TemplateId = groupId,
        Type = SpawnGroupTemplateType.Creature, // unused by DeleteAll; anything but `Any`
    };

    private ISpawnGroupTemplate TemplateRow(uint groupId, HashSet<SpawnGroupMember> members)
    {
        if (GetDetails(groupId) is { } d)
            return new AbstractSpawnGroupTemplateAdvanced
            {
                Id = groupId,
                Name = d.Name,
                Type = d.Type,
                MangosFlags = d.Flags,
                MaxCount = d.MaxCount,
                WorldState = d.WorldState,
                WorldStateExpression = d.WorldStateExpression,
                StringId = d.StringId,
                RespawnOverrideMin = d.RespawnOverrideMin,
                RespawnOverrideMax = d.RespawnOverrideMax,
            };
        return new AbstractSpawnGroupTemplate { Id = groupId, Name = names[groupId], Type = TypeOf(members) };
    }

    /// <summary>The core requires non-negative slot ids to be gapless 0..N-1 (see the ObjectMgr
    /// LoadSpawnGroups fixup); normalize before saving so the DB never triggers that error.</summary>
    private static void NormalizeSlots(SpawnGroupDetails d)
    {
        var inFormation = d.MemberSlots.Where(kv => kv.Value.SlotId >= 0)
            .OrderBy(kv => kv.Value.SlotId).ToList();
        for (int i = 0; i < inFormation.Count; ++i)
        {
            var slot = inFormation[i].Value;
            slot.SlotId = i;
            d.MemberSlots[inFormation[i].Key] = slot;
        }
    }

    public IQuery? BuildSaveQuery()
    {
        if (dirty.Count == 0)
            return null;

        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        foreach (var gid in dirty)
        {
            if (deletedGroups.Contains(gid))
            {
                // the whole group is gone: delete every table's rows, insert nothing
                // (Try* are no-ops on cores without the table)
                Add(templateGen.TryDelete(new AbstractSpawnGroupTemplate { Id = gid, Name = "" }));
                Add(spawnGen.TryDeleteAll(GroupKeyRow(gid)));
                Add(formationGen.TryDeleteAll(new AbstractSpawnGroupFormation { Id = gid }));
                Add(randomEntryGen.TryDeleteAll(new AbstractSpawnGroupRandomEntry { GroupId = gid }));
                Add(linkedGroupGen.TryDeleteAll(new AbstractSpawnGroupLinkedGroup { GroupId = gid }));
                Add(squadGen.TryDeleteAll(new AbstractSpawnGroupSquadMember { GroupId = gid }));
                continue;
            }

            var cur = current.TryGetValue(gid, out var c) ? c : new HashSet<SpawnGroupMember>();
            var d = GetDetails(gid);

            if (d != null)
            {
                NormalizeSlots(d);
                if (d.Formation != null)
                    d.Formation.Spread = Math.Clamp(d.Formation.Spread, -15f, 15f); // core-enforced range
            }

            // idempotent per-group rewrite: DELETE everything first, then INSERT the current state.
            // The full template row is rewritten whenever advanced properties are editable (they may
            // have changed); on basic cores only brand new groups write a template.
            if (d != null || newGroups.Contains(gid))
            {
                var tpl = TemplateRow(gid, cur);
                Add(templateGen.TryDelete(tpl));
                Add(templateGen.TryInsert(tpl));
            }

            Add(spawnGen.TryDeleteAll(GroupKeyRow(gid)));
            if (cur.Count > 0)
                Add(spawnGen.TryBulkInsert(cur.Select(m => Row(gid, m)).ToList()));

            if (d != null)
            {
                var formationKey = new AbstractSpawnGroupFormation { Id = gid };
                Add(formationGen.TryDeleteAll(formationKey));
                if (d.Formation is { } f)
                    Add(formationGen.TryInsert(new AbstractSpawnGroupFormation
                    {
                        Id = gid,
                        FormationType = f.Shape,
                        Spread = f.Spread,
                        Options = f.Options,
                        PathId = f.PathId,
                        MovementType = f.MovementType,
                        Comment = f.Comment,
                    }));

                Add(randomEntryGen.TryDeleteAll(new AbstractSpawnGroupRandomEntry { GroupId = gid }));
                if (d.RandomEntries.Count > 0)
                    Add(randomEntryGen.TryBulkInsert(d.RandomEntries.Select(e => (ISpawnGroupRandomEntry)
                        new AbstractSpawnGroupRandomEntry
                        {
                            GroupId = gid, Entry = e.Entry, MinCount = e.MinCount, MaxCount = e.MaxCount, Chance = e.Chance,
                        }).ToList()));

                Add(linkedGroupGen.TryDeleteAll(new AbstractSpawnGroupLinkedGroup { GroupId = gid }));
                if (d.LinkedGroups.Count > 0)
                    Add(linkedGroupGen.TryBulkInsert(d.LinkedGroups.Distinct().Select(l => (ISpawnGroupLinkedGroup)
                        new AbstractSpawnGroupLinkedGroup { GroupId = gid, LinkedGroupId = l }).ToList()));

                Add(squadGen.TryDeleteAll(new AbstractSpawnGroupSquadMember { GroupId = gid }));
                if (d.Squads.Count > 0)
                    Add(squadGen.TryBulkInsert(d.Squads.Select(s => (ISpawnGroupSquadMember)
                        new AbstractSpawnGroupSquadMember
                        {
                            GroupId = gid, SquadId = s.SquadId, Guid = s.Guid, Entry = s.Entry,
                        }).ToList()));
            }
        }

        return multi?.Close();
    }

    public async Task Save()
    {
        var query = BuildSaveQuery();
        if (query == null)
            return;

        await mainThread.Schedule(async () =>
        {
            await mySqlExecutor.ExecuteSql(query);
            return true;
        });

        // hand the just-saved groups to the session (full app only; the bridge upserts each per-group
        // KEY-ONLY item — the session query re-reads the group's template + membership from the DB,
        // which the live save above just wrote)
        var items = dirty.Select(gid => new SpawnGroupsSolutionItem { GroupId = gid }).ToList();
        eventAggregator.GetEvent<SpawnGroupsSavedEvent>().Publish(items);

        // commit in-memory: originals now match currents; deleted groups are gone for good
        foreach (var gid in dirty)
        {
            if (deletedGroups.Contains(gid))
                original.Remove(gid);
            else
                original[gid] = current.TryGetValue(gid, out var c) ? new HashSet<SpawnGroupMember>(c) : new HashSet<SpawnGroupMember>();
        }
        dirty.Clear();
        newGroups.Clear();
        deletedGroups.Clear();
        Revision++;
    }
}
