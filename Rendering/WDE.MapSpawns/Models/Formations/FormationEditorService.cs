using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using Prism.Events;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Tasks;
using WDE.MapSpawns.Models.Solution;
using WDE.MapSpawns.ViewModels;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Formations;

public class FormationEditorService : IFormationEditorService
{
    private readonly ISpawnsContainer spawnsContainer;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IDatabaseProvider databaseProvider;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IMainThread mainThread;
    private readonly IEventAggregator eventAggregator;
    private readonly IQueryGenerator<ICreatureFormation> formationGen;

    // guid -> creature spawn, rebuilt per map; endpoint positions are read live from these.
    private readonly Dictionary<uint, CreatureSpawnInstance> creatureByGuid = new();

    // formations loaded off-thread land here first, merged into LoadedFormations on the engine thread.
    private readonly ConcurrentQueue<EditableFormation> pendingAdds = new();

    // leader guids whose group changed by a deletion (so save rewrites the whole group).
    private readonly HashSet<uint> dirtyLeaders = new();

    // reused each frame to gather the leader groups currently shown in the editor list.
    private readonly HashSet<uint> shownLeadersScratch = new();

    // last live positions (guid -> pos) + leader orientations, used by SyncConstraints to tell whether
    // the leader or a member was moved this frame. leaderStateScratch is reused (no per-frame alloc).
    private readonly Dictionary<uint, Vector3> prevPos = new();
    private readonly Dictionary<uint, float> prevLeaderOri = new();
    private readonly Dictionary<uint, (Vector3 pos, float ori, bool moved)> leaderStateScratch = new();
    private const float MoveEpsSq = 0.02f * 0.02f; // ignore < 2cm jitter
    private const float OriEps = 0.0017f;          // ignore < ~0.1deg jitter

    public ObservableCollection<EditableFormation> LoadedFormations { get; } = new();
    public EditableFormation? Selected { get; set; }
    public bool ToolEnabled { get; set; }
    public int LoadedMap { get; private set; } = -1;

    public bool DragActive { get; set; }
    public Vector3 DragFrom { get; set; }
    public Vector3 DragTo { get; set; }
    public bool DragSnapped { get; set; }

    public bool IsSupported => formationGen.TableName != null;

    public bool AnyDirty => dirtyLeaders.Count > 0 || LoadedFormations.Any(f => f.IsDirty);

    public FormationEditorService(ISpawnsContainer spawnsContainer,
        ISpawnSelectionService spawnSelectionService,
        IDatabaseProvider databaseProvider,
        IMySqlExecutor mySqlExecutor,
        IMainThread mainThread,
        IEventAggregator eventAggregator,
        IQueryGenerator<ICreatureFormation> formationGen)
    {
        this.spawnsContainer = spawnsContainer;
        this.spawnSelectionService = spawnSelectionService;
        this.databaseProvider = databaseProvider;
        this.mySqlExecutor = mySqlExecutor;
        this.mainThread = mainThread;
        this.eventAggregator = eventAggregator;
        this.formationGen = formationGen;
    }

    /// <summary>
    /// The subset of <see cref="LoadedFormations"/> shown in the editor list (the world still draws
    /// them all). Like the waypoints editor's "loaded paths", a group is listed when it belongs to the
    /// selected spawn OR it has unsaved changes - so a group stays "open" after the spawn is deselected
    /// as long as it's dirty. Fills <paramref name="output"/> (self-rows excluded).
    /// </summary>
    public void CollectListedFormations(List<EditableFormation> output)
    {
        output.Clear();
        shownLeadersScratch.Clear();

        // dirty groups stay listed regardless of selection (edited rows, or groups a deletion touched)
        foreach (var leader in dirtyLeaders)
            shownLeadersScratch.Add(leader);
        foreach (var f in LoadedFormations)
            if (f.IsDirty)
                shownLeadersScratch.Add(f.LeaderGuid);

        // the selected spawn's group: whether it's the leader or any member, show the whole group
        uint selectedGuid = SelectedSpawnGuid;
        if (selectedGuid != 0)
            foreach (var f in LoadedFormations)
                if (f.LeaderGuid == selectedGuid || f.MemberGuid == selectedGuid)
                    shownLeadersScratch.Add(f.LeaderGuid);

        foreach (var f in LoadedFormations)
            if (!f.IsLeaderSelfRow && shownLeadersScratch.Contains(f.LeaderGuid))
                output.Add(f);
    }

    private uint SelectedSpawnGuid =>
        spawnSelectionService.SelectedSpawn.Value is CreatureSpawnInstance creature ? creature.Guid : 0;

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    public async Task LoadForMap(int mapId)
    {
        LoadedMap = mapId;
        LoadedFormations.Clear();
        Selected = null;
        dirtyLeaders.Clear();
        prevPos.Clear();
        prevLeaderOri.Clear();

        creatureByGuid.Clear();
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
            if (spawn is CreatureSpawnInstance creature)
                creatureByGuid[creature.Guid] = creature;

        var all = await databaseProvider.GetCreatureFormations();
        foreach (var row in all)
        {
            // keep only links whose leader AND member are creatures on this map (self-rows qualify too)
            if (!creatureByGuid.ContainsKey(row.LeaderGuid) || !creatureByGuid.ContainsKey(row.MemberGuid))
                continue;
            pendingAdds.Enqueue(new EditableFormation(row.LeaderGuid, row.MemberGuid, row.Dist,
                row.Angle, row.GroupAi, row.Point1, row.Point2));
        }
    }

    public void PumpPendingLoads()
    {
        while (pendingAdds.TryDequeue(out var f))
        {
            if (LoadedFormations.Any(x => x.MemberGuid == f.MemberGuid))
                continue;
            LoadedFormations.Add(f);
        }
    }

    /// <summary>Resolves a creature spawn by guid. The per-map dictionary is a snapshot taken at
    /// map load, so spawns placed AFTER it (session-created pending spawns) miss - fall back to a
    /// container scan and cache the hit, so they become formation-editable immediately.</summary>
    private CreatureSpawnInstance? TryGetCreature(uint guid)
    {
        if (creatureByGuid.TryGetValue(guid, out var creature))
            return creature;

        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            if (spawn is CreatureSpawnInstance c && c.Guid == guid)
            {
                creatureByGuid[guid] = c;
                return c;
            }
        }
        return null;
    }

    public void CollectMemberCreatures(uint leaderGuid, List<CreatureSpawnInstance> output)
    {
        foreach (var f in LoadedFormations)
        {
            if (f.LeaderGuid != leaderGuid || f.IsLeaderSelfRow)
                continue;
            if (TryGetCreature(f.MemberGuid) is { } member)
                output.Add(member);
        }
    }

    public bool TryGetCreaturePosition(uint guid, out Vector3 pos)
    {
        if (TryGetCreature(guid) is { } creature)
        {
            pos = LivePos(creature);
            return true;
        }
        pos = default;
        return false;
    }

    // The live world position of the spawned object (its entity's LocalToWorld), so arrows track an
    // object being dragged. Falls back to the DB spawn position before the object is rendered.
    private static Vector3 LivePos(SpawnInstance spawn) => spawn.WorldObject?.Position ?? spawn.Position;

    // The live facing (entity yaw, WoW-convention radians) of the leader, falling back to the DB
    // orientation before it is rendered.
    private static float LiveOri(CreatureSpawnInstance creature) =>
        creature.Creature?.Orientation ?? creature.Orientation;

    public bool TryGetEndpoints(EditableFormation f, out Vector3 leaderPos, out Vector3 memberPos)
    {
        memberPos = default;
        return TryGetCreaturePosition(f.LeaderGuid, out leaderPos) &&
               TryGetCreaturePosition(f.MemberGuid, out memberPos);
    }

    public EditableFormation? Add(uint memberGuid, uint leaderGuid)
    {
        if (TryGetCreature(leaderGuid) is not { } leader ||
            TryGetCreature(memberGuid) is not { } member)
            return null;

        // a member belongs to exactly one leader (memberGUID is the PK) - drop any prior link
        var existing = LoadedFormations.FirstOrDefault(x => x.MemberGuid == memberGuid);
        if (existing != null)
        {
            dirtyLeaders.Add(existing.LeaderGuid);
            LoadedFormations.Remove(existing);
        }

        float leaderFacing = LiveOri(leader);
        ComputeGeometry(LivePos(leader), leaderFacing, LivePos(member), out var dist, out var angleDeg);
        var formation = new EditableFormation(leaderGuid, memberGuid, dist, angleDeg, 0, 0, 0, dirty: true);
        LoadedFormations.Add(formation);
        Selected = formation;

        // auto-rotate the member to its in-formation facing (during formation movement it travels
        // parallel to the leader, i.e. faces the leader's heading), and seed the constraint cache so
        // this placement isn't mistaken for a manual member move next frame.
        if (member.Creature != null)
            member.Creature.Orientation = leaderFacing;
        prevPos[memberGuid] = LivePos(member);
        return formation;
    }

    public void Remove(EditableFormation f)
    {
        dirtyLeaders.Add(f.LeaderGuid);
        LoadedFormations.Remove(f);
        if (Selected == f)
            Selected = null;
    }

    /// <summary>
    /// Keeps the formation data and the spawn placements in sync as objects are dragged in the editor,
    /// each frame:
    ///  - move a MEMBER -> recompute its dist/angle from its new spot (the member is the free DOF).
    ///  - move/rotate a LEADER -> dist/angle stay; every member is repositioned to its formation slot
    ///    around the leader and re-oriented to the leader's facing (the whole formation drags along).
    /// A leader move takes precedence over a member move in the same frame.
    /// </summary>
    public void SyncConstraints()
    {
        if (LoadedFormations.Count == 0)
            return;

        // pass 1: per-leader live state + whether it moved/rotated since last frame
        leaderStateScratch.Clear();
        for (int i = 0; i < LoadedFormations.Count; ++i)
        {
            uint leaderGuid = LoadedFormations[i].LeaderGuid;
            if (leaderStateScratch.ContainsKey(leaderGuid) ||
                !creatureByGuid.TryGetValue(leaderGuid, out var leader))
                continue;

            var pos = LivePos(leader);
            float ori = LiveOri(leader);
            bool moved = prevPos.TryGetValue(leaderGuid, out var prev) &&
                         (DistSq(pos, prev) > MoveEpsSq ||
                          (prevLeaderOri.TryGetValue(leaderGuid, out var prevOri) && AngleDiff(ori, prevOri) > OriEps));
            leaderStateScratch[leaderGuid] = (pos, ori, moved);
        }

        // pass 2: apply per member
        for (int i = 0; i < LoadedFormations.Count; ++i)
        {
            var f = LoadedFormations[i];
            if (f.IsLeaderSelfRow ||
                !leaderStateScratch.TryGetValue(f.LeaderGuid, out var ls) ||
                !creatureByGuid.TryGetValue(f.MemberGuid, out var member))
                continue;

            var memberPos = LivePos(member);
            if (ls.moved)
            {
                // leader dragged: re-place the member at its formation slot, keeping dist/angle
                float leaderDz = prevPos.TryGetValue(f.LeaderGuid, out var pl) ? ls.pos.Z - pl.Z : 0f;
                var slot = MemberSlot(ls.pos, ls.ori, f.Dist, f.Angle, memberPos.Z + leaderDz);
                if (member.WorldObject != null)
                    member.WorldObject.Position = slot;
                if (member.Creature != null)
                    member.Creature.Orientation = ls.ori;
                prevPos[f.MemberGuid] = slot;
            }
            else if (prevPos.TryGetValue(f.MemberGuid, out var prevMember) && DistSq(memberPos, prevMember) > MoveEpsSq)
            {
                // member dragged: recompute dist/angle from its new position
                ComputeGeometry(ls.pos, ls.ori, memberPos, out var dist, out var angleDeg);
                f.SetGeometry(dist, angleDeg);
                prevPos[f.MemberGuid] = memberPos;
            }
            else
            {
                prevPos[f.MemberGuid] = memberPos;
            }
        }

        // pass 3: commit leader caches
        foreach (var (leaderGuid, ls) in leaderStateScratch)
        {
            prevPos[leaderGuid] = ls.pos;
            prevLeaderOri[leaderGuid] = ls.ori;
        }
    }

    private static float DistSq(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    private static float AngleDiff(float a, float b)
    {
        float d = MathF.Abs(a - b) % (2f * MathF.PI);
        return d > MathF.PI ? 2f * MathF.PI - d : d;
    }

    /// <summary>
    /// Matches how TrinityCore stores + applies creature_formations:
    ///  - dist  = 2D leader↔member distance (GetExactDist2d).
    ///  - angle = the member's bearing AROUND THE LEADER, measured relative to the LEADER's facing,
    ///            in DEGREES [0,360). FormationMovementGenerator places the member at
    ///            leaderPos + dist·dir(followAngle + π + leaderHeading), and CreatureGroups inverts
    ///            the relative angle by π - so followAngle = atan2(member-leader) + π - leaderFacing.
    /// Only the LEADER's orientation is involved; the member's own facing does not affect placement.
    /// </summary>
    private static void ComputeGeometry(Vector3 leaderPos, float leaderFacing, Vector3 memberPos,
        out float dist, out float angleDeg)
    {
        float dx = memberPos.X - leaderPos.X;
        float dy = memberPos.Y - leaderPos.Y;
        dist = MathF.Sqrt(dx * dx + dy * dy);

        float angleRad = MathF.Atan2(dy, dx) + MathF.PI - leaderFacing;
        angleDeg = angleRad * (180f / MathF.PI) % 360f;
        if (angleDeg < 0)
            angleDeg += 360f;
    }

    // Inverse of ComputeGeometry: where the member sits for a given dist/angle around the leader -
    // exactly the spot FormationMovementGenerator drives it to (leader + dist·dir(angle+π+facing)).
    private static Vector3 MemberSlot(Vector3 leaderPos, float leaderFacing, float dist, float angleDeg, float z)
    {
        float ang = angleDeg * (MathF.PI / 180f) + MathF.PI + leaderFacing;
        return new Vector3(leaderPos.X + dist * MathF.Cos(ang), leaderPos.Y + dist * MathF.Sin(ang), z);
    }

    /// <summary>Every leader group touched by an edit or a deletion (rewritten wholesale on save).</summary>
    private HashSet<uint> CollectDirtyLeaders()
    {
        var leaders = new HashSet<uint>(dirtyLeaders);
        foreach (var f in LoadedFormations)
            if (f.IsDirty)
                leaders.Add(f.LeaderGuid);
        return leaders;
    }

    public IQuery? BuildSaveQuery()
    {
        var leaders = CollectDirtyLeaders();
        if (leaders.Count == 0)
            return null;

        IMultiQuery? multi = null;
        foreach (var leader in leaders)
        {
            var delete = formationGen.TryDelete(new EditableFormation(leader, leader, 0, 0, 0, 0, 0));
            if (delete == null)
                throw new Exception("The current core has no creature_formations SQL provider.");

            multi ??= Queries.BeginTransaction(delete.Database);
            multi.Add(delete);

            var members = LoadedFormations.Where(f => f.LeaderGuid == leader)
                .Cast<ICreatureFormation>().ToList();
            if (members.Count > 0)
            {
                var insert = formationGen.TryBulkInsert(members);
                if (insert != null)
                    multi.Add(insert);
            }
        }

        return multi!.Close();
    }

    public async Task Save()
    {
        var leaders = CollectDirtyLeaders();
        var query = BuildSaveQuery();
        if (query == null)
            return;

        await mainThread.Schedule(async () =>
        {
            await mySqlExecutor.ExecuteSql(query);
            return true;
        });

        // hand the just-saved formations to the session (full app only; the bridge upserts each
        // per-leader KEY-ONLY item — the session query re-reads the group's rows from the DB, which
        // the live save above just wrote)
        var items = leaders.Select(leader => new FormationsSolutionItem { LeaderGuid = leader }).ToList();
        eventAggregator.GetEvent<FormationsSavedEvent>().Publish(items);

        dirtyLeaders.Clear();
        foreach (var f in LoadedFormations)
            f.ClearDirty();
    }
}
