using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Hexa.NET.ImGui;
using Prism.Commands;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using TheEngine.PhysicsSystem;
using TheEngine.Structures;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.Models.Waypoints;
using WDE.MapSpawns.ViewModels;
using WDE.MpqReader.Structures;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.SpawnGroups;

/// <summary>
/// The Spawn-group tool: clicking a spawn that belongs to a group SELECTS that group for editing
/// (members get cyan decals, its formation shows ghost slot markers); clicking ungrouped spawns
/// toggles them into a pending selection (green decals) which the inspector turns into a new
/// group or adds to an existing one. The full group editor UI lives in
/// <see cref="SpawnGroupInspector"/>.
/// </summary>
public class SpawnGroupEditorModule : IGameModule
{
    private readonly Engine engine;
    private readonly IGameContext gameContext;
    private readonly ISpawnEditorToolService toolService;
    private readonly ISpawnGroupEditorService service;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IRenderManager renderManager;
    private readonly IEntityManager entityManager;
    private readonly IInputManager inputManager;
    private readonly IWorldInteractionService interaction;
    private readonly IGameViewOverlayService overlays;
    private readonly IWaypointEditorService waypointService;
    private readonly RaycastSystem raycastSystem;
    private readonly ICachedDatabaseProvider cachedDatabase;
    private readonly IWorldSpawnEditService editService;
    private readonly SpawnGroupInspector inspector;

    // internal: SpawnGroupInspector draws a legend with the exact same colors
    internal static readonly Vector4 PendingDecalColor = new(0.30f, 0.90f, 0.45f, 0.9f);  // green
    internal static readonly Vector4 GroupDecalColor = new(0.15f, 0.70f, 1.00f, 0.9f);    // cyan
    internal static readonly Vector4 LeaderDecalColor = new(1.00f, 0.75f, 0.15f, 0.95f);  // gold
    internal static readonly Vector4 GhostColor = new(0.75f, 0.55f, 1.00f, 0.9f);         // violet
    private static readonly Vector4 GhostLinkColor = new(0.75f, 0.55f, 1.00f, 0.35f);
    private const float DecalVerticalHalf = 5.0f;
    private const float DecalRadius = 2.0f;
    // huge groups would mean thousands of decal entities updated per frame - draw the first
    // MaxDecals and say so in the inspector (DecalCapNotice)
    private const int MaxDecals = 256;

    private readonly List<SpawnInstance> pending = new();
    private readonly List<Entity> decals = new();
    private int enabledDecals; // decals[0..enabledDecals) are enabled - lets DisableDecalsFrom no-op instead of touching every decal every inactive frame
    private ITexture? decalTexture;

    private uint selectedGroupId;
    /// <summary>The group being edited (0 = none - the pending/new-group flow is active).</summary>
    public uint SelectedGroupId
    {
        get => selectedGroupId;
        set => selectedGroupId = value;
    }

    /// <summary>The pending world-selection (ungrouped spawns clicked with the tool active).</summary>
    public List<SpawnInstance> Pending => pending;

    /// <summary>Non-null while more spawns qualify for decals than are drawn - shown in the
    /// inspector so missing markers aren't mistaken for missing membership.</summary>
    public string? DecalCapNotice { get; private set; }

    // guid -> live spawn, rebuilt when membership or the world changes (avoids an O(spawns) scan per row)
    private readonly Dictionary<SpawnGroupMember, SpawnInstance> spawnCache = new();
    private int spawnCacheRevision = -1;
    private int frameCounter;
    private int nextMissRebuildFrame;

    // leader crowns are drawn by the shared overhead-icon renderer (StatusIconsManager) - we just
    // push it the set of leader CreatureInstances each frame while the tool is active
    private readonly List<CreatureInstance> leaderScratch = new();
    private bool leaderCrownsPushed;

    // formation-path jumps requested by the inspector (UI) - executed here on the engine thread
    private readonly Queue<uint> openPathQueue = new();

    // context-menu commands run on the UI thread - engine/service mutations hop back via this queue
    private readonly ConcurrentQueue<Action> engineActions = new();
    // built at right-press on the engine thread, consumed by GenerateContextMenu on the UI thread
    private List<(string, ICommand, object?)>? contextMenuItems;

    public object? ViewModel => null;

    public SpawnGroupEditorModule(Engine engine,
        IGameContext gameContext,
        ISpawnEditorToolService toolService,
        ISpawnGroupEditorService service,
        ISpawnsContainer spawnsContainer,
        IRenderManager renderManager,
        IEntityManager entityManager,
        IInputManager inputManager,
        IWorldInteractionService interaction,
        IGameViewOverlayService overlays,
        IWaypointEditorService waypointService,
        RaycastSystem raycastSystem,
        ICachedDatabaseProvider cachedDatabase,
        IWorldSpawnEditService editService,
        EntryPickerService entryPicker,
        ISpawnSelectionService spawnSelectionService)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.engine = engine;
        this.gameContext = gameContext;
        this.toolService = toolService;
        this.service = service;
        this.spawnsContainer = spawnsContainer;
        this.renderManager = renderManager;
        this.entityManager = entityManager;
        this.inputManager = inputManager;
        this.interaction = interaction;
        this.overlays = overlays;
        this.waypointService = waypointService;
        this.raycastSystem = raycastSystem;
        this.cachedDatabase = cachedDatabase;
        this.editService = editService;
        inspector = new SpawnGroupInspector(service, this, entryPicker);
    }

    public void Initialize()
    {
        decalTexture = SpawnDecalTexture.BuildCircleTexture(gameContext);
        phantomLayer = renderManager.RegisterRenderLayer("Spawn group formation phantoms");
        overlays.SetSection(SpawnEditorTool.SpawnGroup, inspector);
    }

    public void Dispose()
    {
        overlays.SetSection(SpawnEditorTool.SpawnGroup, null);
        ClearLeaderCrowns();
        ClearPhantoms();
        renderManager.UnregisterRenderLayer(phantomLayer);
        foreach (var d in decals)
            if (entityManager.Exist(d))
                entityManager.DestroyEntity(d);
        decals.Clear();
        if (decalTexture != null)
            gameContext.Engine.TextureManager.DisposeTexture(decalTexture);
        decalTexture = null;
    }

    public void Update(float delta)
    {
        service.PumpPendingLoads();
        SyncMap();
        ProcessOpenPathRequests();
        while (engineActions.TryDequeue(out var action))
            action();
        frameCounter++;

        // leader crowns show at all times (not only in the spawn-group tool), so they're pushed to
        // the overhead-icon renderer every frame regardless of the active tool
        SyncLeaderCrowns();

        // the inspector's "Edit" button asks us to open a specific group (see ISpawnGroupEditorService)
        if (service.RequestedEditGroup is { } requested)
        {
            service.RequestedEditGroup = null;
            SelectedGroupId = requested;
            pending.Clear();
        }

        if (toolService.ActiveTool != SpawnEditorTool.SpawnGroup)
        {
            if (pending.Count > 0)
                pending.Clear();
            DisableDecalsFrom(0);
            ghostSlots.Clear();
            ClearPhantoms();
            return;
        }

        DrawDecals();
        ComputeFormationSlots();
        UpdatePhantoms();

        if (interaction.IsCaptured)
            return;

        if (inputManager.Mouse.HasJustClicked(MouseButton.Left))
        {
            var spawn = PickSpawnUnderCursor();
            if (spawn != null)
            {
                var member = new SpawnGroupMember(spawn is CreatureSpawnInstance, spawn.Guid);
                if (service.GroupOf(member) is { } groupId)
                    SelectedGroupId = groupId; // clicking a grouped spawn edits its group
                else
                    Toggle(spawn);
                spawnSelectionService.SelectedSpawn.Value = spawn; // shared selection + highlight
                interaction.UsePointerThisFrame();
            }
        }

        // right press over a spawn: remember the group context menu for the release-time
        // Avalonia menu (and target the spawn with the shared selection)
        if (inputManager.Mouse.HasJustClicked(MouseButton.Right))
        {
            var spawn = PickSpawnUnderCursor();
            contextMenuItems = spawn == null ? null : BuildContextItems(spawn);
            if (spawn != null)
                spawnSelectionService.SelectedSpawn.Value = spawn;
        }
    }

    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        if (toolService.ActiveTool != SpawnEditorTool.SpawnGroup)
            return null;
        var items = contextMenuItems;
        contextMenuItems = null;
        return items;
    }

    private ICommand GameCommand(Action action) => new DelegateCommand(() => engineActions.Enqueue(action));

    private List<(string, ICommand, object?)> BuildContextItems(SpawnInstance spawn)
    {
        var items = new List<(string, ICommand, object?)>();
        var member = new SpawnGroupMember(spawn is CreatureSpawnInstance, spawn.Guid);

        if (service.GroupOf(member) is { } groupId)
        {
            string name = service.GroupNames.TryGetValue(groupId, out var n) ? n : "";
            items.Add(($"{DescribeSpawn(spawn)} — group {groupId} {name}", AlwaysDisabledCommand.Command, null));
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            if (SelectedGroupId != groupId)
                items.Add(("Edit this spawn group", GameCommand(() => SelectedGroupId = groupId), null));
            var details = service.GetDetails(groupId);
            if (details != null && member.IsCreature && service.SupportsFormations &&
                details.Type == SpawnGroupTemplateType.Creature && details.SlotOf(member.Guid).SlotId != 0)
                items.Add(("Make formation leader", GameCommand(() => MakeLeader(groupId, member.Guid)), null));
            items.Add(("Remove from the group", GameCommand(() => service.RemoveMember(groupId, member)), null));
        }
        else
        {
            items.Add((DescribeSpawn(spawn), AlwaysDisabledCommand.Command, null));
            items.Add(("-", AlwaysDisabledCommand.Command, null));
            if (SelectedGroupId != 0 && service.GroupNames.TryGetValue(SelectedGroupId, out var selName))
            {
                uint targetGroup = SelectedGroupId;
                items.Add(($"Add to group {targetGroup} {selName}",
                    GameCommand(() => service.AddToGroup(targetGroup, new[] { member })), null));
            }
            bool inPending = pending.Any(p => p.Guid == spawn.Guid &&
                                              p is CreatureSpawnInstance == member.IsCreature);
            items.Add((inPending ? "Remove from the selection" : "Add to the selection",
                GameCommand(() => Toggle(spawn)), null));
        }

        return items;
    }

    /// <summary>Slot 0 moves to the given member; the previous leader takes over the member's old
    /// slot (or joins the formation at the end when the member had none).</summary>
    public void MakeLeader(uint groupId, uint guid)
    {
        if (service.GetDetails(groupId) is not { } details)
            return;

        var newLeaderOldSlot = details.SlotOf(guid).SlotId;

        uint oldLeader = details.MemberSlots.FirstOrDefault(kv => kv.Value.SlotId == 0).Key;
        if (oldLeader != 0 && oldLeader != guid)
        {
            var old = details.SlotOf(oldLeader);
            old.SlotId = newLeaderOldSlot > 0
                ? newLeaderOldSlot
                : details.MemberSlots.Values.Max(s => s.SlotId) + 1;
            details.MemberSlots[oldLeader] = old;
        }

        var slot = details.SlotOf(guid);
        slot.SlotId = 0;
        details.MemberSlots[guid] = slot;
        service.NotifyDetailsChanged(groupId);
    }

    // DrawSphere/DrawLine issue immediate draws - only legal inside the engine's rendering loop
    // (Update runs before it and asserts), so the guide lines/fallback markers render here from
    // the slot data Update computed
    public void Render(float delta)
    {
        if (toolService.ActiveTool == SpawnEditorTool.SpawnGroup)
            DrawFormationGuides();
    }

    public void RenderGUI()
    {
        DrawGhostBadges();
    }

    /// <summary>Feeds the shared overhead-icon renderer the current group leaders (slot 0) so it
    /// draws a gold crown over each - the same billboard mechanism as the quest/gossip/AI icons,
    /// instead of hand-projecting a crown into the ImGui drawlist. Called every frame regardless of
    /// the active tool (crowns show at all times), so leadership edits and streamed-in leaders both
    /// show up; the crown is toggleable via the status-icons dropdown like the other icons.</summary>
    private void SyncLeaderCrowns()
    {
        leaderScratch.Clear();
        if (service.SupportsFormations)
        {
            foreach (var groupId in service.GroupNames.Keys)
            {
                if (service.GetDetails(groupId) is not { } details)
                    continue;

                uint leaderGuid = 0;
                foreach (var (guid, slot) in details.MemberSlots)
                {
                    if (slot.SlotId == 0)
                    {
                        leaderGuid = guid;
                        break;
                    }
                }
                if (leaderGuid == 0)
                    continue;

                if (FindSpawn(new SpawnGroupMember(true, leaderGuid)) is CreatureSpawnInstance leader &&
                    leader.Creature is { } instance)
                    leaderScratch.Add(instance);
            }
        }

        gameContext.StatusIconsManager.SetDynamicIconOwners(StatusIconsManager.StatusIcon.Leader, leaderScratch);
        leaderCrownsPushed = leaderScratch.Count > 0;
    }

    /// <summary>Clears the leader crowns (tool left / module disposed). No-op once cleared, so the
    /// per-frame inactive path doesn't keep re-clearing the whole icon list.</summary>
    private void ClearLeaderCrowns()
    {
        if (!leaderCrownsPushed)
            return;
        leaderScratch.Clear();
        gameContext.StatusIconsManager.SetDynamicIconOwners(StatusIconsManager.StatusIcon.Leader, leaderScratch);
        leaderCrownsPushed = false;
    }

    // ------------------------------------------------------------- inspector services ------------

    public static string DescribeSpawn(SpawnInstance s) => s switch
    {
        CreatureSpawnInstance c => $"{c.CreatureTemplate.Name} #{c.Guid}",
        GameObjectSpawnInstance g => $"{g.GameObjectTemplate.Name} #{g.Guid}",
        _ => $"#{s.Guid}",
    };

    public List<SpawnGroupMember> CollectPending()
    {
        var list = new List<SpawnGroupMember>(pending.Count);
        foreach (var s in pending)
            list.Add(new SpawnGroupMember(s is CreatureSpawnInstance, s.Guid));
        return list;
    }

    private readonly ISpawnSelectionService spawnSelectionService;

    /// <summary>Panel-row -> 3D sync: highlights the spawn with the shared selection (decal) and
    /// optionally flies the camera to it.</summary>
    public void HighlightSpawn(SpawnInstance spawn, bool flyTo)
    {
        spawnSelectionService.SelectedSpawn.Value = spawn;
        if (flyTo && spawn.WorldObject != null)
            gameContext.SetMap((int)gameContext.CurrentMapId, spawn.WorldObject.Position);
    }

    /// <summary>Resolves a group member to its live spawn instance (null when not loaded).
    /// Backed by a guid dictionary; a full container sweep happens only on a membership change or
    /// a lookup miss - and miss-triggered sweeps are throttled (a member whose spawn never loads
    /// must not degrade into a full scan per frame; FindSpawn runs per member per frame).</summary>
    public SpawnInstance? FindSpawn(SpawnGroupMember member)
    {
        if (spawnCacheRevision != service.Revision)
            RebuildSpawnCache();

        if (spawnCache.TryGetValue(member, out var s))
            return s;

        // miss: the world may have streamed the spawn in since the last rebuild
        if (frameCounter >= nextMissRebuildFrame)
        {
            RebuildSpawnCache();
            if (spawnCache.TryGetValue(member, out s))
                return s;
        }
        return null;
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private void RebuildSpawnCache()
    {
        spawnCacheRevision = service.Revision;
        nextMissRebuildFrame = frameCounter + 30; // ~0.25-0.5s between miss-triggered sweeps
        spawnCache.Clear();
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
            spawnCache[new SpawnGroupMember(spawn is CreatureSpawnInstance, spawn.Guid)] = spawn;
    }

    public bool CanEditFormationPaths =>
        waypointService.AvailableSources.Contains(WaypointSource.MangosWaypointPath);

    /// <summary>Called from the inspector: open (or create) the group formation's waypoint_path in
    /// the waypoint editor. Allocation/loading is queued and runs on the engine thread.</summary>
    public void RequestOpenFormationPath(SpawnGroupDetails details) => openPathQueue.Enqueue(details.Id);

    private void ProcessOpenPathRequests()
    {
        while (openPathQueue.Count > 0)
        {
            var groupId = openPathQueue.Dequeue();
            if (service.GetDetails(groupId) is { Formation: not null } details)
                OpenFormationPath(details).ListenErrors();
        }
    }

    private async Task OpenFormationPath(SpawnGroupDetails details)
    {
        var formation = details.Formation!;
        int pathId = formation.PathId;
        if (pathId == 0)
        {
            // allocate the next free waypoint_path id for a brand new formation path
            var ids = await waypointService.EnumeratePathIds(WaypointSource.MangosWaypointPath);
            pathId = (int)(ids.Count == 0 ? 1 : ids.Max() + 1);
        }

        var loaded = await waypointService.LoadPath(WaypointSource.MangosWaypointPath, (uint)pathId);

        // awaits from game code already resume on the game loop; this only defers to the next
        // frame so editor state changes land at a frame boundary
        await engine.NextFrame;

        if (formation.PathId != pathId)
        {
            formation.PathId = pathId;
            service.NotifyDetailsChanged(details.Id);
        }
        waypointService.PumpPendingLoads(); // make a freshly loaded path visible right away
        var path = loaded ?? waypointService.CreateNew(WaypointSource.MangosWaypointPath, (uint)pathId);
        waypointService.SelectedPath = path;
        if (path.Points.Count == 0)
            waypointService.EditingPath = path; // fresh path: arm pen mode right away
        toolService.ActiveTool = SpawnEditorTool.Waypoint;
    }

    private void Toggle(SpawnInstance spawn)
    {
        bool isCreature = spawn is CreatureSpawnInstance;
        for (int i = 0; i < pending.Count; ++i)
        {
            if (pending[i].Guid == spawn.Guid && (pending[i] is CreatureSpawnInstance) == isCreature)
            {
                pending.RemoveAt(i);
                return;
            }
        }
        pending.Add(spawn);
    }

    private void SyncMap()
    {
        if (spawnsContainer.IsLoading)
            return;
        var map = spawnsContainer.LoadedMap;
        if (!map.HasValue || map.Value == service.LoadedMap)
            return;
        service.LoadForMap(map.Value).ListenErrors();
    }

    private SpawnInstance? PickSpawnUnderCursor()
    {
        var picked = renderManager.PickObject(inputManager.Mouse.NormalizedPosition);
        if (picked.IsEmpty())
            return null;
        picked = picked.GetRoot(entityManager);
        if (!entityManager.Exist(picked))
            return null;
        if (!entityManager.HasManagedComponent<SpawnInstance>(picked))
            return null;
        return entityManager.GetManagedComponent<SpawnInstance>(picked);
    }

    // -------------------------------------------------------------------- decals -----------------

    private readonly List<SpawnGroupMember> membersScratch = new();

    private void DrawDecals()
    {
        int used = 0;
        int skipped = 0;

        void Draw(Vector3 pos, Vector4 color)
        {
            if (used < MaxDecals)
                SetDecal(used++, pos, color);
            else
                skipped++;
        }

        // pending selection = green
        foreach (var s in pending)
            Draw(s.WorldObject?.Position ?? s.Position, PendingDecalColor);

        // the selected group's members = cyan (leader gold)
        if (SelectedGroupId != 0)
        {
            var details = service.GetDetails(SelectedGroupId);
            service.CollectMembers(SelectedGroupId, membersScratch);
            foreach (var m in membersScratch)
            {
                if (FindSpawn(m) is not { } spawn)
                    continue;
                bool leader = details != null && details.SlotOf(m.Guid).SlotId == 0;
                Draw(spawn.WorldObject?.Position ?? spawn.Position, leader ? LeaderDecalColor : GroupDecalColor);
            }
        }

        DisableDecalsFrom(used);
        DecalCapNotice = skipped > 0
            ? $"{used + skipped} spawns qualify for markers - drawing only {MaxDecals}"
            : null;
    }

    private void SetDecal(int index, Vector3 pos, Vector4 color)
    {
        while (decals.Count <= index)
        {
            var e = entityManager.CreateEntity(entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Decal>(), "Spawn group decal");
            ref var nd = ref entityManager.GetComponent<Decal>(e);
            nd.Disabled = true;
            nd.FadeAngleCos = 0.3f;
            nd.Albedo = decalTexture;
            decals.Add(e);
        }
        var entity = decals[index];
        ref var decal = ref entityManager.GetComponent<Decal>(entity);
        decal.Disabled = false;
        decal.Color = color;
        entityManager.GetComponent<LocalToWorld>(entity).Matrix =
            Matrix.CreateScale(DecalRadius, DecalRadius, DecalVerticalHalf) * Matrix.CreateTranslation(pos);
        if (index >= enabledDecals)
            enabledDecals = index + 1;
    }

    private void DisableDecalsFrom(int index)
    {
        for (int i = index; i < enabledDecals; ++i)
            entityManager.GetComponent<Decal>(decals[i]).Disabled = true;
        if (enabledDecals > index)
            enabledDecals = index;
    }

    // --------------------------------------------------------- formation ghost preview -----------

    private struct GhostSlot
    {
        public uint Guid;
        public int SlotId;
        public Vector3 Pos;
        public float Heading;
        public Vector3? MemberPos;  // the member's live position (guide line), null when not loaded
        public uint MemberEntry;    // 0 when the member spawn isn't loaded (no phantom possible)
        public bool HasPhantom;     // a translucent model stands on the slot (skip the fallback sphere)
    }

    private readonly List<GhostSlot> ghostSlots = new();
    private readonly List<KeyValuePair<uint, SpawnGroupMemberSlot>> followersScratch = new();
    private int followersRevision = -1;
    private uint followersGroup;
    private Vector3 ghostLeaderPos;

    // translucent preview models standing on the slots, keyed by member guid
    private RenderLayer phantomLayer;
    private readonly Dictionary<uint, CreatureInstance> phantoms = new();
    private readonly HashSet<uint> phantomLoads = new();
    private int phantomGeneration;
    private const float GroundSnapProbe = 60f; // how far above/below the leader slots may land

    /// <summary>Computes the formation slot world positions around the live leader with the exact
    /// core math (<see cref="SpawnGroupFormationMath"/>). Runs in Update - the phantoms' entity
    /// transforms are updated from this; Render only draws guides from the same data.</summary>
    private void ComputeFormationSlots()
    {
        ghostSlots.Clear();
        if (SelectedGroupId == 0 || service.GetDetails(SelectedGroupId) is not { Formation: { } formation } details)
            return;

        // resolve the leader (slot 0) and its live transform
        uint leaderGuid = 0;
        foreach (var (guid, slot) in details.MemberSlots)
        {
            if (slot.SlotId == 0)
            {
                leaderGuid = guid;
                break;
            }
        }
        if (leaderGuid == 0)
            return; // no leader assigned - the inspector shows a warning instead

        if (FindSpawn(new SpawnGroupMember(true, leaderGuid)) is not CreatureSpawnInstance leader)
            return;
        var leaderPos = leader.WorldObject?.Position ?? leader.Position;
        float heading = leader.Creature?.Orientation ?? leader.Orientation;
        ghostLeaderPos = leaderPos;

        // followers ordered by slot; the shape functions are 1-based per-follower. Cached -
        // this runs every frame, the slots only change with the service revision
        if (followersRevision != service.Revision || followersGroup != SelectedGroupId)
        {
            followersRevision = service.Revision;
            followersGroup = SelectedGroupId;
            followersScratch.Clear();
            foreach (var kv in details.MemberSlots)
                if (kv.Value.SlotId > 0)
                    followersScratch.Add(kv);
            followersScratch.Sort(static (a, b) => a.Value.SlotId.CompareTo(b.Value.SlotId));
        }
        var followers = followersScratch;

        foreach (var (guid, slot) in followers)
        {
            var (angle, dist) = SpawnGroupFormationMath.FollowerOffset(
                formation.Shape, slot.SlotId, followers.Count, formation.Spread);
            float worldAngle = heading + angle;
            var ghostPos = leaderPos + new Vector3(MathF.Cos(worldAngle) * dist, MathF.Sin(worldAngle) * dist, 0);
            ghostPos.Z = SnapToGroundZ(ghostPos);

            var member = FindSpawn(new SpawnGroupMember(true, guid)) as CreatureSpawnInstance;
            ghostSlots.Add(new GhostSlot
            {
                Guid = guid,
                SlotId = slot.SlotId,
                Pos = ghostPos,
                Heading = heading,
                MemberPos = member?.WorldObject?.Position ?? member?.Position,
                MemberEntry = member?.Entry ?? 0,
            });
        }
    }

    /// <summary>The nearest walkable surface around the slot (bridges/slopes), defaulting to the
    /// computed height when nothing is hit.</summary>
    private float SnapToGroundZ(Vector3 pos)
    {
        var hits = raycastSystem.RaycastAll(new Ray(pos.WithZ(pos.Z + GroundSnapProbe), Vectors.Down),
            pos.WithZ(pos.Z - GroundSnapProbe), Collisions.COLLISION_MASK_STATIC);
        if (hits == null || hits.Count == 0)
            return pos.Z;

        float bestZ = pos.Z;
        float bestDelta = float.MaxValue;
        foreach (var (_, hitPos) in hits)
        {
            float delta = MathF.Abs(hitPos.Z - pos.Z);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestZ = hitPos.Z;
            }
        }
        return bestZ;
    }

    // ------------------------------------------------------------- slot phantoms -----------------

    /// <summary>Keeps one translucent creature model standing on every slot whose member is loaded:
    /// creates missing phantoms (async model load), disposes phantoms whose slot went away and
    /// follows the slot transforms every frame.</summary>
    private void UpdatePhantoms()
    {
        if (ghostSlots.Count == 0)
        {
            ClearPhantoms();
            return;
        }

        // drop phantoms whose slot/member disappeared (manual scan - closures here would
        // allocate every frame)
        List<uint>? remove = null;
        foreach (var guid in phantoms.Keys)
        {
            bool slotAlive = false;
            foreach (var s in ghostSlots)
            {
                if (s.Guid == guid && s.MemberEntry != 0)
                {
                    slotAlive = true;
                    break;
                }
            }
            if (!slotAlive)
                (remove ??= new List<uint>()).Add(guid);
        }
        if (remove != null)
        {
            foreach (var guid in remove)
            {
                phantoms[guid].Dispose();
                phantoms.Remove(guid);
            }
        }

        for (int i = 0; i < ghostSlots.Count; ++i)
        {
            var slot = ghostSlots[i];
            if (slot.MemberEntry == 0)
                continue;

            if (phantoms.TryGetValue(slot.Guid, out var phantom))
            {
                phantom.Position = slot.Pos;
                phantom.Orientation = slot.Heading;
                slot.HasPhantom = true;
                ghostSlots[i] = slot;
            }
            else if (!phantomLoads.Contains(slot.Guid))
            {
                phantomLoads.Add(slot.Guid);
                LoadPhantom(slot.Guid, slot.MemberEntry, phantomGeneration).ListenErrors();
            }
        }
    }

    private async Task LoadPhantom(uint guid, uint entry, int generation)
    {
        try
        {
            var template = cachedDatabase.GetCachedCreatureTemplate(entry) ?? await cachedDatabase.GetCreatureTemplate(entry);
            if (template == null)
                return;

            var creature = new CreatureInstance(gameContext, template, null, phantomLayer);
            await creature.Load();

            // the preview may be gone by now (tool/group switched, generation bumped)
            if (generation != phantomGeneration || !ghostSlots.Any(s => s.Guid == guid))
            {
                creature.Dispose();
                return;
            }

            creature.SetTranslucent(true); // ghost dither (instances own per-instance material clones)
            creature.Animation = M2AnimationType.Stand;
            phantoms[guid] = creature;
        }
        finally
        {
            phantomLoads.Remove(guid);
        }
    }

    private void ClearPhantoms()
    {
        if (phantoms.Count == 0)
        {
            phantomGeneration++;
            return;
        }
        phantomGeneration++; // invalidates in-flight loads
        foreach (var phantom in phantoms.Values)
            phantom.Dispose();
        phantoms.Clear();
    }

    /// <summary>Guide lines + fallback markers for the computed slots (phantom models do the real
    /// visualization once loaded). Render-phase only.</summary>
    private void DrawFormationGuides()
    {
        if (ghostSlots.Count == 0)
            return;

        renderManager.DrawSphere(ghostLeaderPos + Vectors.Up * 0.2f, 0.35f, LeaderDecalColor);

        foreach (var slot in ghostSlots)
        {
            renderManager.DrawLine(ghostLeaderPos, slot.Pos, GhostLinkColor);
            if (!slot.HasPhantom)
                renderManager.DrawSphere(slot.Pos + Vectors.Up * 0.2f, 0.45f, GhostColor);
            if (slot.MemberPos is { } memberPos)
                renderManager.DrawLine(memberPos, slot.Pos, GhostColor with { W = 0.55f });
        }
    }

    // -------------------------------------------------- move members onto their slots ------------

    /// <summary>True when the preview is live (leader + at least one slotted follower).</summary>
    public bool CanMoveMembersToSlots => ghostSlots.Count > 0;

    /// <summary>Teleports every slotted member's spawn to its computed formation position (facing
    /// the leader's heading) - live world objects move immediately, and the edit is persisted as a
    /// normal undoable spawn move when the full editor is attached.</summary>
    public void MoveMembersToSlots()
    {
        foreach (var slot in ghostSlots)
        {
            if (FindSpawn(new SpawnGroupMember(true, slot.Guid)) is not CreatureSpawnInstance member)
                continue;

            if (member.WorldObject is { } worldObject)
                worldObject.Position = slot.Pos;
            if (member.Creature is { } creature)
                creature.Orientation = slot.Heading;

            if (editService.IsAvailable)
                editService.MoveSpawn(true, slot.Guid, slot.Pos, slot.Heading);
        }
    }

    // slot-number chips over the ghost markers, same projection as the waypoint badges
    private void DrawGhostBadges()
    {
        if (ghostSlots.Count == 0 || toolService.ActiveTool != SpawnEditorTool.SpawnGroup)
            return;

        if (!ImGui.Begin("3D"))
        {
            ImGui.End();
            return;
        }

        var dl = ImGui.GetWindowDrawList();
        var view = engine.GameView.ViewRect;
        var camera = engine.CameraManager.MainCamera;
        var viewProj = camera.ViewMatrix * camera.ProjectionMatrix;

        foreach (var slot in ghostSlots)
        {
            var (slotId, pos) = (slot.SlotId, slot.Pos);
            var clip = Vector4.Transform(new Vector4(pos.X, pos.Y, pos.Z, 1f), viewProj);
            if (clip.W <= 0)
                continue;
            float nx = (clip.X / clip.W + 1f) * 0.5f;
            float ny = 1f - (clip.Y / clip.W + 1f) * 0.5f;
            if (nx < 0 || nx > 1 || ny < 0 || ny > 1)
                continue;

            string label = slotId.ToString();
            var textSize = ImGui.CalcTextSize(label);
            var pad = new System.Numerics.Vector2(4, 1);
            var anchor = new System.Numerics.Vector2(view.X + nx * view.Width, view.Y + ny * view.Height);
            var min = anchor + new System.Numerics.Vector2(-textSize.X * 0.5f - pad.X, -textSize.Y - 12 - pad.Y * 2);
            var max = min + textSize + pad * 2;

            dl.AddRectFilled(min, max, ImGui.GetColorU32(ImGuiCol.WindowBg, 0.75f), 3f);
            dl.AddText(min + pad, ImGui.GetColorU32(ImGuiCol.Text, 0.9f), label);
        }

        ImGui.End();
    }
}
