using System;
using System.Collections.Generic;
using TheEngine.Components;
using WDE.Common.Database;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.Pools;
using WDE.MapSpawns.ViewModels;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawns.Rendering.Pools;

/// <summary>
/// The Spawn-pool tool: clicking a spawn that belongs to a pool (directly, or via entry-wide
/// pooling) SELECTS that pool for editing; clicking unpooled spawns toggles them into a pending
/// selection (green decals) which the inspector turns into a new pool or adds to an existing one.
/// The selected pool's direct members get cyan decals, spawns pooled entry-wide get violet ones
/// and its child pools' members dim blue ones. The full pool editor UI lives in
/// <see cref="PoolInspector"/>.
/// </summary>
public class PoolEditorModule : IGameModule
{
    private readonly IGameContext gameContext;
    private readonly ISpawnEditorToolService toolService;
    private readonly IPoolEditorService service;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IRenderManager renderManager;
    private readonly IEntityManager entityManager;
    private readonly IInputManager inputManager;
    private readonly IWorldInteractionService interaction;
    private readonly IGameViewOverlayService overlays;
    private readonly PoolInspector inspector;

    // internal: PoolInspector draws a legend with the exact same colors
    internal static readonly Vector4 PendingDecalColor = new(0.30f, 0.90f, 0.45f, 0.9f);   // green
    internal static readonly Vector4 MemberDecalColor = new(0.15f, 0.70f, 1.00f, 0.9f);    // cyan
    internal static readonly Vector4 EntryPooledDecalColor = new(0.75f, 0.55f, 1.00f, 0.9f); // violet
    internal static readonly Vector4 ChildMemberDecalColor = new(0.25f, 0.45f, 0.90f, 0.55f); // dim blue
    private const float DecalVerticalHalf = 5.0f;
    private const float DecalRadius = 2.0f;
    // huge pools (entry-wide ones especially) would mean thousands of decal entities updated per
    // frame - draw the first MaxDecals and say so in the inspector (DecalCapNotice)
    private const int MaxDecals = 256;

    private readonly List<SpawnInstance> pending = new();
    private readonly List<Entity> decals = new();
    private int enabledDecals; // decals[0..enabledDecals) are enabled - lets DisableDecalsFrom no-op instead of touching every decal every inactive frame
    private ITexture? decalTexture;

    /// <summary>The pool being edited (0 = none - the pending/new-pool flow is active).</summary>
    public uint SelectedPoolId { get; set; }

    /// <summary>The pending world-selection (unpooled spawns clicked with the tool active).</summary>
    public List<SpawnInstance> Pending => pending;

    /// <summary>Non-null while more spawns qualify for decals than are drawn - shown in the
    /// inspector so missing markers aren't mistaken for missing membership.</summary>
    public string? DecalCapNotice { get; private set; }

    // member -> live spawn + entry -> live spawns, rebuilt when membership or the world changes
    // (avoids an O(spawns) scan per lookup)
    private readonly Dictionary<PoolMember, SpawnInstance> spawnCache = new();
    private readonly Dictionary<PoolEntryKey, List<SpawnInstance>> entrySpawnCache = new();
    private int spawnCacheRevision = -1;
    private int frameCounter;
    private int nextMissRebuildFrame;

    public object? ViewModel => null;

    public PoolEditorModule(IGameContext gameContext,
        ISpawnEditorToolService toolService,
        IPoolEditorService service,
        ISpawnsContainer spawnsContainer,
        IRenderManager renderManager,
        IEntityManager entityManager,
        IInputManager inputManager,
        IWorldInteractionService interaction,
        IGameViewOverlayService overlays,
        ICachedDatabaseProvider cachedDatabase,
        EntryPickerService entryPicker,
        ISpawnSelectionService spawnSelectionService)
    {
        this.spawnSelectionService = spawnSelectionService;
        this.gameContext = gameContext;
        this.toolService = toolService;
        this.service = service;
        this.spawnsContainer = spawnsContainer;
        this.renderManager = renderManager;
        this.entityManager = entityManager;
        this.inputManager = inputManager;
        this.interaction = interaction;
        this.overlays = overlays;
        inspector = new PoolInspector(service, this, cachedDatabase, entryPicker);
    }

    public void Initialize()
    {
        decalTexture = SpawnDecalTexture.BuildCircleTexture(gameContext);
        overlays.SetSection(SpawnEditorTool.Pool, inspector);
    }

    public void Dispose()
    {
        overlays.SetSection(SpawnEditorTool.Pool, null);
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
        frameCounter++;

        if (toolService.ActiveTool != SpawnEditorTool.Pool)
        {
            if (pending.Count > 0)
                pending.Clear();
            DisableDecalsFrom(0);
            return;
        }

        DrawDecals();

        if (interaction.IsCaptured)
            return;

        if (inputManager.Mouse.HasJustClicked(MouseButton.Left))
        {
            var spawn = PickSpawnUnderCursor();
            if (spawn != null)
            {
                bool isCreature = spawn is CreatureSpawnInstance;
                var member = new PoolMember(isCreature, spawn.Guid);
                // clicking a pooled spawn edits its pool - direct membership wins over entry-wide
                if (service.PoolOf(member) is { } poolId)
                    SelectedPoolId = poolId;
                else if (service.PoolOfEntry(new PoolEntryKey(isCreature, spawn.Entry)) is { } entryPoolId)
                    SelectedPoolId = entryPoolId;
                else
                    Toggle(spawn);
                interaction.UsePointerThisFrame();
            }
        }
    }

    public void Render(float delta)
    {
    }

    public void RenderGUI()
    {
    }

    // ------------------------------------------------------------- inspector services ------------

    public static string DescribeSpawn(SpawnInstance s) => s switch
    {
        CreatureSpawnInstance c => $"{c.CreatureTemplate.Name} #{c.Guid}",
        GameObjectSpawnInstance g => $"{g.GameObjectTemplate.Name} #{g.Guid}",
        _ => $"#{s.Guid}",
    };

    public List<PoolMember> CollectPending()
    {
        var list = new List<PoolMember>(pending.Count);
        foreach (var s in pending)
            list.Add(new PoolMember(s is CreatureSpawnInstance, s.Guid));
        return list;
    }

    /// <summary>Resolves a pool member to its live spawn instance (null when not loaded). Backed by
    /// a guid dictionary; a full container sweep happens only on a pool change or a lookup miss -
    /// and miss-triggered sweeps are throttled (a member whose spawn never loads must not degrade
    /// into a full scan per frame; FindSpawn runs per member per frame).</summary>
    private readonly ISpawnSelectionService spawnSelectionService;

    /// <summary>Panel-row -> 3D sync: highlights the spawn with the shared selection (decal) and
    /// optionally flies the camera to it.</summary>
    public void HighlightSpawn(SpawnInstance spawn, bool flyTo)
    {
        spawnSelectionService.SelectedSpawn.Value = spawn;
        if (flyTo && spawn.WorldObject != null)
            gameContext.SetMap((int)gameContext.CurrentMapId, spawn.WorldObject.Position);
    }

    public SpawnInstance? FindSpawn(PoolMember member)
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

    /// <summary>All loaded spawns of the entry (empty when none are streamed in). The list is owned
    /// by the cache - read it, don't keep it.</summary>
    public IReadOnlyList<SpawnInstance> FindSpawnsOfEntry(PoolEntryKey entry)
    {
        if (spawnCacheRevision != service.Revision)
            RebuildSpawnCache();
        return entrySpawnCache.TryGetValue(entry, out var list) ? list : Array.Empty<SpawnInstance>();
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private void RebuildSpawnCache()
    {
        spawnCacheRevision = service.Revision;
        nextMissRebuildFrame = frameCounter + 30; // ~0.25-0.5s between miss-triggered sweeps
        spawnCache.Clear();
        entrySpawnCache.Clear();
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var spawn in spawnsScratch)
        {
            bool isCreature = spawn is CreatureSpawnInstance;
            spawnCache[new PoolMember(isCreature, spawn.Guid)] = spawn;
            var entryKey = new PoolEntryKey(isCreature, spawn.Entry);
            if (!entrySpawnCache.TryGetValue(entryKey, out var list))
                entrySpawnCache[entryKey] = list = new List<SpawnInstance>();
            list.Add(spawn);
        }
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

    private readonly List<PoolMember> membersScratch = new();
    private readonly List<uint> childrenScratch = new();

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

        if (SelectedPoolId != 0)
        {
            // the selected pool's direct members = cyan
            service.CollectMembers(SelectedPoolId, membersScratch);
            foreach (var m in membersScratch)
            {
                if (FindSpawn(m) is not { } spawn)
                    continue;
                Draw(spawn.WorldObject?.Position ?? spawn.Position, MemberDecalColor);
            }

            // loaded spawns of entry-wide members = violet
            if (service.GetDetails(SelectedPoolId) is { } details)
            {
                foreach (var entry in details.EntryMembers.Keys)
                {
                    foreach (var spawn in FindSpawnsOfEntry(entry))
                        Draw(spawn.WorldObject?.Position ?? spawn.Position, EntryPooledDecalColor);
                }
            }

            // direct children's members = dim blue (one level - enough to see the nesting)
            service.CollectChildren(SelectedPoolId, childrenScratch);
            foreach (var child in childrenScratch)
            {
                service.CollectMembers(child, membersScratch);
                foreach (var m in membersScratch)
                {
                    if (FindSpawn(m) is not { } spawn)
                        continue;
                    Draw(spawn.WorldObject?.Position ?? spawn.Position, ChildMemberDecalColor);
                }
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
                .WithComponentData<Decal>(), "Spawn pool decal");
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
}
