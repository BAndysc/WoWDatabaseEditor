using System;
using System.Collections.Generic;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.Models.SpawnGroups;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Rendering;

/// <summary>
/// Highlights the rest of a spawn group when one of its members is selected: a differently
/// colored decal on every other member that lives on the current map. Reads membership from
/// <see cref="ISpawnGroupEditorService"/>, so it reflects unsaved group edits too.
/// Owned and driven by <see cref="SpawnViewer"/>.
/// </summary>
public class SpawnGroupVisualizer
{
    private readonly IGameContext gameContext;
    private readonly IEntityManager entityManager;
    private readonly ISpawnSelectionService selectionService;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly ISpawnGroupEditorService groupService;

    private static readonly Vector4 GroupDecalColor = new(1.0f, 0.55f, 0.12f, 0.9f); // orange, distinct from the aqua selection
    // huge groups would mean thousands of decal entities and per-frame updates - cap at the
    // closest members and say so (CapNotice); re-picked when the camera moves far enough
    private const int MaxDecals = 128;
    private const float CapRefreshDistance = 40.0f;
    private const float DecalVerticalHalf = 5.0f;
    private const float DecalRadiusFallback = 2.0f;
    private const float DecalRadiusMin = 1.0f;
    private const float DecalRadiusMax = 20.0f;
    private const float DecalRadiusPadding = 1.15f;

    private ITexture? decalTexture;
    private readonly List<Entity> decals = new();
    private bool initialized;

    // current-map guid -> live instance
    private int? instanceLookupMap;
    private readonly Dictionary<(bool isCreature, uint guid), SpawnInstance> instanceLookup = new();
    private readonly List<SpawnGroupMember> memberScratch = new(); // reused so Update stays alloc-free

    // the decal set only depends on (selected spawn, group membership revision, loaded map); it is
    // rebuilt when one of those changes and the per-frame Update degrades to plain position compares
    // over this list - no per-member ECS/dictionary traffic in the steady state
    private struct ActiveDecal
    {
        public SpawnInstance Instance;
        public Vector3 LastPos;
        public bool HasBounds; // once WorldMeshBounds is seen the radius is final (until a rebuild)
    }
    private readonly List<ActiveDecal> active = new();
    private int activeCount;
    private SpawnInstance? builtForSelection;
    private int builtForRevision = -1;
    private int? builtForMap;

    private readonly List<(float distSq, SpawnInstance inst, Vector3 pos)> candidateScratch = new();
    private static readonly Comparison<(float distSq, SpawnInstance inst, Vector3 pos)> ByDistance =
        (a, b) => a.distSq.CompareTo(b.distSq);
    private bool capped;
    private Vector3 builtForCameraPos;

    /// <summary>Non-null while the selected group has more members than are shown - surfaced in
    /// the Select inspector so the missing decals aren't mistaken for missing membership.</summary>
    public string? CapNotice { get; private set; }

    public SpawnGroupVisualizer(IGameContext gameContext,
        IEntityManager entityManager,
        ISpawnSelectionService selectionService,
        ISpawnsContainer spawnsContainer,
        ISpawnGroupEditorService groupService)
    {
        this.gameContext = gameContext;
        this.entityManager = entityManager;
        this.selectionService = selectionService;
        this.spawnsContainer = spawnsContainer;
        this.groupService = groupService;
    }

    public void Initialize()
    {
        decalTexture = BuildCircleTexture();
        initialized = true;
    }

    public void Dispose()
    {
        foreach (var d in decals)
            if (entityManager.Exist(d))
                entityManager.DestroyEntity(d);
        decals.Clear();
        active.Clear();
        activeCount = 0;
        builtForSelection = null;
        builtForRevision = -1;
        builtForMap = null;
        capped = false;
        CapNotice = null;
        if (decalTexture != null)
            gameContext.Engine.TextureManager.DisposeTexture(decalTexture);
        decalTexture = null;
        initialized = false;
    }

    public void Update(float delta)
    {
        // HasData flips only together with a Revision bump, so the revision check covers it
        var sel = initialized && groupService.HasData ? selectionService.SelectedSpawn.Value : null;

        if (!ReferenceEquals(sel, builtForSelection) ||
            groupService.Revision != builtForRevision ||
            spawnsContainer.LoadedMap != builtForMap ||
            // capped set: which members are "closest" depends on the camera - re-pick when it moved far
            (capped && Vector3.DistanceSquared(CameraPosition, builtForCameraPos) >
                CapRefreshDistance * CapRefreshDistance))
        {
            builtForSelection = sel;
            builtForRevision = groupService.Revision;
            builtForMap = spawnsContainer.LoadedMap;
            RebuildActiveDecals(sel);
        }

        for (int i = 0; i < active.Count; i++)
        {
            var a = active[i];
            var pos = a.Instance.WorldObject?.Position ?? a.Instance.Position;
            bool moved = pos != a.LastPos;
            if (!moved && a.HasBounds)
                continue;

            // the member moved (drag/undo) or its model may have just finished loading (bounds
            // appear late) - only now is the ECS touched
            float radius = RadiusForSpawn(a.Instance, out bool hasBounds);
            if (!moved && !hasBounds)
                continue; // still waiting for bounds, nothing to update yet

            a.LastPos = pos;
            a.HasBounds = hasBounds;
            active[i] = a;
            entityManager.GetComponent<LocalToWorld>(decals[i]).Matrix =
                Matrix.CreateScale(radius, radius, DecalVerticalHalf) * Matrix.CreateTranslation(pos);
        }
    }

    private Vector3 CameraPosition => gameContext.Engine.CameraManager.MainCamera.Transform.Position;

    private void RebuildActiveDecals(SpawnInstance? sel)
    {
        var cameraPos = CameraPosition;
        builtForCameraPos = cameraPos;
        candidateScratch.Clear();

        if (sel != null)
        {
            var selMember = new SpawnGroupMember(sel is CreatureSpawnInstance, sel.Guid);
            if (groupService.GroupOf(selMember) is uint groupId)
            {
                EnsureInstanceLookup();
                groupService.CollectMembers(groupId, memberScratch);
                foreach (var m in memberScratch)
                {
                    if (m == selMember)
                        continue; // the selected member keeps the aqua selection decal
                    if (!instanceLookup.TryGetValue((m.IsCreature, m.Guid), out var inst))
                        continue; // not present on the current map

                    var pos = inst.WorldObject?.Position ?? inst.Position;
                    candidateScratch.Add((Vector3.DistanceSquared(pos, cameraPos), inst, pos));
                }
            }
        }

        capped = candidateScratch.Count > MaxDecals;
        CapNotice = capped
            ? $"{candidateScratch.Count} group members - showing the {MaxDecals} closest"
            : null;
        if (capped)
            candidateScratch.Sort(ByDistance);

        int used = Math.Min(candidateScratch.Count, MaxDecals);
        for (int i = 0; i < used; i++)
        {
            var (_, inst, pos) = candidateScratch[i];
            var e = EnsureDecal(i);
            float radius = RadiusForSpawn(inst, out bool hasBounds);
            entityManager.GetComponent<Decal>(e).Disabled = false;
            entityManager.GetComponent<LocalToWorld>(e).Matrix =
                Matrix.CreateScale(radius, radius, DecalVerticalHalf) * Matrix.CreateTranslation(pos);

            var entry = new ActiveDecal { Instance = inst, LastPos = pos, HasBounds = hasBounds };
            if (i < active.Count)
                active[i] = entry;
            else
                active.Add(entry);
        }

        // disable the previous selection's leftovers once - not every frame
        for (int i = used; i < activeCount; i++)
            SetDecalDisabled(i);
        activeCount = used;
        if (active.Count > used)
            active.RemoveRange(used, active.Count - used);
        candidateScratch.Clear(); // don't pin thousands of instances until the next rebuild
    }

    // reused every traversal so FlatTreeList.GetChildren never allocates iterators (see GetChildren(List<C>))
    private readonly List<SpawnInstance> spawnsScratch = new();

    private void EnsureInstanceLookup()
    {
        if (instanceLookupMap == spawnsContainer.LoadedMap)
            return;
        instanceLookup.Clear();
        instanceLookupMap = spawnsContainer.LoadedMap;
        if (spawnsContainer.LoadedMap == null)
            return;
        spawnsScratch.Clear();
        spawnsContainer.Spawns.GetChildren(spawnsScratch);
        foreach (var s in spawnsScratch)
            instanceLookup[(s is CreatureSpawnInstance, s.Guid)] = s;
    }

    private Entity EnsureDecal(int index)
    {
        while (decals.Count <= index)
        {
            var e = entityManager.CreateEntity(entityManager.NewArchetype()
                .WithComponentData<LocalToWorld>()
                .WithComponentData<Decal>(), "Spawn group decal");
            ref var d = ref entityManager.GetComponent<Decal>(e);
            d.Disabled = true;
            d.Color = GroupDecalColor;
            d.FadeAngleCos = 0.3f;
            d.Albedo = decalTexture;
            decals.Add(e);
        }
        return decals[index];
    }

    private void SetDecalDisabled(int index)
    {
        if (index >= decals.Count)
            return;
        entityManager.GetComponent<Decal>(decals[index]).Disabled = true;
    }

    /// <summary><paramref name="hasBounds"/> = the radius came from real world bounds; false means
    /// the model is still loading and the caller should keep polling until they appear.</summary>
    private float RadiusForSpawn(SpawnInstance spawn, out bool hasBounds)
    {
        var worldObject = spawn.WorldObject;
        if (worldObject != null)
        {
            var entity = worldObject.WorldObjectEntity;
            if (entityManager.Exist(entity) && entityManager.HasComponent<WorldMeshBounds>(entity))
            {
                var box = entityManager.GetComponent<WorldMeshBounds>(entity).box;
                float horizontalHalf = 0.5f * MathF.Max(box.Size.X, box.Size.Y);
                hasBounds = true;
                return Math.Clamp(horizontalHalf * DecalRadiusPadding, DecalRadiusMin, DecalRadiusMax);
            }
        }
        hasBounds = false;
        return DecalRadiusFallback;
    }

    private ITexture BuildCircleTexture()
    {
        const int size = 256;
        var pixels = new Vector4[size * size];
        for (int y = 0; y < size; ++y)
        {
            for (int x = 0; x < size; ++x)
            {
                float u = (x + 0.5f) / size * 2f - 1f;
                float v = (y + 0.5f) / size * 2f - 1f;
                float dist = MathF.Sqrt(u * u + v * v);
                // a ring so it reads differently from the solid selection decal
                float inner = SmoothStep(0.55f, 0.68f, dist);
                float outer = 1f - SmoothStep(0.9f, 1f, dist);
                pixels[y * size + x] = new Vector4(1f, 1f, 1f, inner * outer);
            }
        }
        return gameContext.Engine.TextureManager.CreateTexture(pixels, size, size);
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Math.Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
