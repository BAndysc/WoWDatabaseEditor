using System.Runtime.InteropServices;
using Prism.Ioc;
using TheEngine;
using TheEngine.Components;
using TheEngine.Data;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Resources;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers.Entities;

namespace WDE.MapRenderer.Managers;

/// <summary>
/// WoW-style status icons floating above NPC heads (quest !, quest ?, gossip bubble, AI gear...).
///
/// Built for zero steady-state cost: each creature registers ONCE when it spawns (the icon mask is
/// computed from its template + the bulk-loaded quest relations - no lookups ever happen again) and
/// the per-frame work is one linear walk over the registered list (distance cull + O(1) component
/// reads) feeding a single instanced billboard draw (status_icon shader, per-instance bindless
/// icon textures - no atlas, no per-icon draw calls, no allocations).
///
/// Quest starter/ender relations arrive from the database on a background task and are pumped in
/// on the game thread (<see cref="Update"/>), which re-stamps already-registered creatures once.
/// Which icons show is <see cref="IGameProperties.ShowStatusIcons"/> +
/// <see cref="IGameProperties.StatusIconsHiddenMask"/> (the view-settings toolbar dropdown).
/// The glyphs themselves are SDFs drawn in status_icon.frag (no texture assets).
/// </summary>
public class StatusIconsManager : System.IDisposable
{
    // world-space cull distance, a bit beyond the 50u nameplate range so quest givers can be
    // spotted from afar (the whole point of the ! and ?)
    private const float VisibilityDistance = 100;
    private const float VisibilityDistanceSq = VisibilityDistance * VisibilityDistance;

    public enum StatusIcon
    {
        QuestGiver = 0,
        QuestEnder = 1,
        Gossip = 2,
        Ai = 3,
        // "dynamic" icons aren't derived from the creature template - an editor sets them per-creature
        // at runtime via SetDynamicIconOwners. They're still full status icons otherwise (listed in
        // the dropdown, obey ShowStatusIcons + the hidden mask).
        Leader = 4,    // spawn group formation leader crown (set by the spawn-group tool)
        Waypoints = 5, // creature has a movement path assigned (set by the waypoints tool)
    }

    /// <summary>Bit registry driving both the renderer and the settings dropdown; adding an icon
    /// kind = one entry here + a bit source (in <see cref="StaticMaskOf"/>, the quest pump, or
    /// <see cref="SetDynamicIconOwners"/>) + an SDF glyph in status_icon.frag (icons are drawn
    /// procedurally - the client's 32px BLPs looked terrible scaled up).</summary>
    public static readonly (StatusIcon icon, string label)[] Icons =
    {
        (StatusIcon.QuestGiver, "Quest giver (!)"),
        (StatusIcon.QuestEnder, "Quest ender (?)"),
        (StatusIcon.Gossip, "Gossip"),
        (StatusIcon.Ai, "Scripted AI"),
        (StatusIcon.Leader, "Group leader"),
        (StatusIcon.Waypoints, "Has waypoints"),
    };

    public static uint BitOf(StatusIcon icon) => 1u << (int)icon;

    private struct Entry
    {
        public Entity Entity;
        public uint CreatureEntry;
        public uint StaticMask; // gossip/AI bits from the template - never change
        public uint Mask;       // StaticMask | quest bits (re-stamped when relations arrive)
        public uint Dynamic;    // editor-set bits (leader crown...) - bypass ShowStatusIcons/hidden mask
        public float HeightOffset;
    }

    private sealed class QuestSets
    {
        public required HashSet<uint> Starters;
        public required HashSet<uint> Enders;
    }

    private readonly IGameContext gameContext;
    private readonly Engine engine;
    private readonly IGameProperties properties;
    private readonly IDatabaseProvider? databaseProvider;

    private readonly Dictionary<CreatureInstance, int> indexOf = new();
    private readonly List<Entry> entries = new();
    private readonly List<CreatureInstance> owners = new(); // parallel to entries, for O(1) swap-remove

    private bool questsRequested;
    private volatile QuestSets? pendingQuestSets;
    private HashSet<uint>? questStarters;
    private HashSet<uint>? questEnders;

    private bool resourcesRequested;
    private IMesh? quad;
    private Material? material;

    // per-frame scratch, reused (Clear keeps capacity)
    private readonly List<Matrix> models = new();
    private readonly List<Int4> drawData = new();

    public StatusIconsManager(IGameContext gameContext,
        Engine engine,
        IGameProperties properties,
        IContainerProvider containerProvider,
        IContainerRegistry registry)
    {
        this.gameContext = gameContext;
        this.engine = engine;
        this.properties = properties;
        // optional: the standalone tester may run without any database at all
        databaseProvider = registry.IsRegistered(typeof(IDatabaseProvider))
            ? containerProvider.Resolve<IDatabaseProvider>()
            : null;
    }

    private uint StaticMaskOf(ICreatureTemplate template)
    {
        uint mask = 0;
        if ((template.NpcFlags & GameDefines.NpcFlags.Gossip) != 0 || template.GossipMenuId != 0)
            mask |= BitOf(StatusIcon.Gossip);
        if (!string.IsNullOrEmpty(template.AIName) || !string.IsNullOrEmpty(template.ScriptName))
            mask |= BitOf(StatusIcon.Ai);
        return mask;
    }

    private uint QuestMaskOf(uint entry)
    {
        uint mask = 0;
        if (questStarters != null && questStarters.Contains(entry))
            mask |= BitOf(StatusIcon.QuestGiver);
        if (questEnders != null && questEnders.Contains(entry))
            mask |= BitOf(StatusIcon.QuestEnder);
        return mask;
    }

    /// <summary>Called once per creature when its 3D entity is built (CreatureInstance.Load);
    /// heightOffset = world-space height of the anchor above the creature's origin.</summary>
    public void Register(CreatureInstance creature, ICreatureTemplate template, float heightOffset)
    {
        if (indexOf.ContainsKey(creature))
            return;
        var staticMask = StaticMaskOf(template);
        indexOf[creature] = entries.Count;
        owners.Add(creature);
        entries.Add(new Entry
        {
            Entity = creature.WorldObjectEntity,
            CreatureEntry = template.Entry,
            StaticMask = staticMask,
            Mask = staticMask | QuestMaskOf(template.Entry),
            HeightOffset = heightOffset,
        });
    }

    public void Unregister(CreatureInstance creature)
    {
        if (!indexOf.Remove(creature, out var index))
            return;
        int last = entries.Count - 1;
        if (index != last)
        {
            entries[index] = entries[last];
            owners[index] = owners[last];
            indexOf[owners[index]] = index;
        }
        entries.RemoveAt(last);
        owners.RemoveAt(last);
    }

    /// <summary>Sets a DYNAMIC (editor-driven) icon bit - one not derived from the creature template -
    /// on exactly the given owners, clearing it from everyone else. The bit renders like any other
    /// status icon (obeys ShowStatusIcons + the hidden mask); only its source is different. Cheap to
    /// call every frame; owners not currently registered are ignored (so a leader whose 3D model
    /// hasn't streamed in yet simply gets its crown once it registers + the next call lands). Pass an
    /// empty collection to clear the icon.</summary>
    public void SetDynamicIconOwners(StatusIcon icon, IReadOnlyCollection<CreatureInstance> owners)
    {
        uint bit = BitOf(icon);
        var span = CollectionsMarshal.AsSpan(entries);
        for (int i = 0; i < span.Length; ++i)
            span[i].Dynamic &= ~bit;
        foreach (var owner in owners)
            if (indexOf.TryGetValue(owner, out var index))
                span[index].Dynamic |= bit;
    }

    public void Update(float delta)
    {
        if (!questsRequested && databaseProvider != null)
        {
            questsRequested = true;
            LoadQuestRelations().ListenErrors();
        }

        var pending = pendingQuestSets;
        if (pending != null)
        {
            pendingQuestSets = null;
            questStarters = pending.Starters;
            questEnders = pending.Enders;
            // one-time re-stamp of everything registered before the DB answered
            var span = CollectionsMarshal.AsSpan(entries);
            for (int i = 0; i < span.Length; ++i)
                span[i].Mask = span[i].StaticMask | QuestMaskOf(span[i].CreatureEntry);
        }
    }

    private async Task LoadQuestRelations()
    {
        var starters = await databaseProvider!.GetAllQuestStarters();
        var enders = await databaseProvider.GetAllQuestEnders();
        if (starters == null && enders == null)
            return;

        QuestSets sets = new() { Starters = new HashSet<uint>(), Enders = new HashSet<uint>() };
        if (starters != null)
            foreach (var relation in starters)
                if (relation.Type == QuestRelationObjectType.Creature)
                    sets.Starters.Add(relation.Entry);
        if (enders != null)
            foreach (var relation in enders)
                if (relation.Type == QuestRelationObjectType.Creature)
                    sets.Enders.Add(relation.Entry);
        pendingQuestSets = sets; // picked up by Update on the game thread
    }

    public void Render()
    {
        if (entries.Count == 0 || !properties.ShowStatusIcons)
            return;

        EnsureResources();
        if (material == null || quad == null)
            return;

        uint enabledMask = ~properties.StatusIconsHiddenMask;
        var entityManager = gameContext.EntityManager;
        var renderManager = engine.RenderManager;
        var cameraPos = engine.CameraManager.MainCamera.Transform.Position;

        models.Clear();
        drawData.Clear();

        var span = CollectionsMarshal.AsSpan(entries);
        for (int i = 0; i < span.Length; ++i)
        {
            ref var e = ref span[i];
            // dynamic (editor-set) bits render just like template/quest bits - subject to the mask
            uint mask = (e.Mask | e.Dynamic) & enabledMask;
            if (mask == 0)
                continue;

            var pos = entityManager.GetComponent<LocalToWorld>(e.Entity).Position;
            if (Vector3.DistanceSquared(pos, cameraPos) > VisibilityDistanceSq)
                continue;

            ref var renderBit = ref entityManager.GetComponent<RenderEnabledBit>(e.Entity);
            if (renderBit.IsForceDisabled())
                continue;
            if (renderBit.Layer > 0 && !renderManager.IsRenderLayerEnabled(renderBit.Layer))
                continue;

            var model = Matrix.CreateTranslation(pos with { Z = pos.Z + e.HeightOffset });
            int count = System.Numerics.BitOperations.PopCount(mask);
            int slot = 0;
            uint bits = mask;
            while (bits != 0)
            {
                int icon = System.Numerics.BitOperations.TrailingZeroCount(bits);
                bits &= bits - 1;
                models.Add(model);
                drawData.Add(new Int4(icon, slot++, count, 0));
            }
        }

        if (models.Count > 0)
            renderManager.RenderInstanced(quad, material, ShaderPassType.Forward, 0,
                CollectionsMarshal.AsSpan(models), CollectionsMarshal.AsSpan(drawData));
    }

    private void EnsureResources()
    {
        if (resourcesRequested)
            return;
        resourcesRequested = true;

        quad = engine.MeshManager.CreateMesh(new MeshData(new[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
        }, null, new[]
        {
            new Vector2(0, 1),
            new Vector2(1, 1),
            new Vector2(1, 0),
            new Vector2(0, 0),
        }, new ushort[] { 0, 1, 2, 2, 3, 0 }));

        var shader = engine.ShaderManager.LoadShader("internalShaders/status_icon.json");
        var pipeline = engine.PipelineManager.CreatePipeline(shader, Veldrid.PrimitiveTopology.TriangleList,
            new Veldrid.GraphicsPipelineDescription
            {
                BlendState = Veldrid.BlendStateDescription.SingleAlphaBlend,
                RasterizerState = Veldrid.RasterizerStateDescription.CullNone,
                // like the nameplates: always on top, never hidden by world geometry
                DepthStencilState = Veldrid.DepthStencilStateDescription.Disabled,
            }, false);
        material = engine.MaterialManager.CreateMaterial(pipeline);
    }

    public void Dispose()
    {
        entries.Clear();
        indexOf.Clear();
        if (quad != null)
            engine.MeshManager.DisposeMesh(quad);
        quad = null;
        material = null;
    }
}
