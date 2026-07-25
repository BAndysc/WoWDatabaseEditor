using System.Diagnostics;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// The built-in stage that draws all ECS mesh-renderer entities: culls and sorts them
/// in <see cref="PrepareFrame"/>, then records the opaque and transparent ranges at the
/// respective render points, batching consecutive identical draws into instanced draws.
/// Also keeps the draw-index-to-entity mapping that object picking reads back.
///
/// The collected/sorted draw list lives in a <see cref="RenderSet"/>. Normally only
/// <see cref="mainSet"/> (culled against the game camera) is built and BOTH views draw it -
/// so the scene view shows exactly what the game view renders. When the scene view opts into
/// its own culling (<see cref="EngineSceneView.OwnCulling"/>), a second <see cref="sceneSet"/>
/// is built against the scene-view camera and the scene view draws that instead.
/// </summary>
internal sealed partial class ObjectDrawRenderStage : IRenderStage
{
    // Everything that describes one camera's collected, sorted, instanced draw list. We keep two:
    // mainSet (game camera) and sceneSet (scene-view camera, only when it culls independently).
    private sealed class RenderSet
    {
        // Note: per-instance shader data (Int4) is read from MeshRenderer.InstanceData directly during rendering.
        public (LocalToWorld, Entity)[] renderersData = new (LocalToWorld, Entity)[10000];
        public MeshRenderer[] renderers = new MeshRenderer[10000];
        public int opaque;
        public int transparent;
        public int totalToDraw;

        // On backends where gl_InstanceIndex = firstInstance + gl_InstanceID (SupportsBaseInstance),
        // these arrays cover the whole [0, totalToDraw) range (opaque + transparent) and are
        // uploaded once per frame; each instanced batch then just offsets into them via
        // firstInstance, so set=1 (material + instancing buffers) stays identical across
        // consecutive same-material batches and the descriptor write is cached.
        public Matrix[] globalModelsArray = new Matrix[1];
        public Matrix[] globalInverseModelsArray = new Matrix[1];
        public uint[] globalObjectIndicesArray = new uint[1];
        public Int4[] globalDrawDataArray = new Int4[1];
        public int[] globalMaterialIndexArray = new int[1];
        public bool globalBuffersReady;
        public bool globalBuffersUploaded;
        public INativeBuffer? globalModelsBuffer;
        public INativeBuffer? globalInverseModelsBuffer;
        public INativeBuffer? globalObjectIndicesBuffer;
        public INativeBuffer? globalDrawDataBuffer;
        public INativeBuffer? globalMaterialIndexBuffer;
    }

    private readonly Engine engine;
    private readonly RenderManager renderManager;

    private readonly Archetype updateWorldBoundsArchetype;
    private readonly Archetype toCullArchetype;
    private readonly Archetype toRenderArchetype;
    private readonly Archetype entitiesSharingRenderingArchetype;
    private readonly Archetype dynamicParentedRenderersArchetype;
    private readonly Archetype dirtMeshRenderersArchetype;

    private readonly Stopwatch boundsUpdate = new();
    private readonly Stopwatch culler = new();
    private readonly Stopwatch sorting = new();
    private readonly Stopwatch drawing = new();

    private readonly RenderSet mainSet = new();
    private readonly RenderSet sceneSet = new();
    // true for the rest of the frame once PrepareSceneFrame built sceneSet; reset by PrepareFrame.
    private bool sceneSetActive;

    // Shadow casters: a SEPARATE opaque set collected without ANY camera cull (all non-force-disabled
    // opaque renderers whose bounds reach into the shadow distance of the camera), so objects
    // behind/beside the camera - or distance-culled for the view - still cast shadows into the view.
    // The jobs deliberately ignore RenderEnabledBit's culled bit (written by CollectInto for the
    // camera) and test only explicit hiding. Built in PrepareFrame alongside the camera set, with
    // its own sort + instancing buffers, and drawn on RenderPoint.Shadow.
    private MeshRenderer[] shadowCasters = new MeshRenderer[1];
    private (LocalToWorld, Entity)[] shadowCastersData = new (LocalToWorld, Entity)[1];
    private int shadowCasterCount;
    private Matrix[] shadowModelsArray = new Matrix[1];
    private Matrix[] shadowInverseModelsArray = new Matrix[1];
    private uint[] shadowObjectIndicesArray = new uint[1];
    private Int4[] shadowDrawDataArray = new Int4[1];
    private int[] shadowMaterialIndexArray = new int[1];
    private float shadowCasterReach;
    private bool shadowBuffersReady;
    private bool shadowBuffersUploaded;
    private INativeBuffer? shadowModelsBuffer;
    private INativeBuffer? shadowInverseModelsBuffer;
    private INativeBuffer? shadowObjectIndicesBuffer;
    private INativeBuffer? shadowDrawDataBuffer;
    private INativeBuffer? shadowMaterialIndexBuffer;

    public string Name => "Objects";

    public RenderPoint RenderPoints => RenderPoint.Opaque | RenderPoint.Transparent | RenderPoint.DepthPrepass | RenderPoint.Shadow;

    internal ObjectDrawRenderStage(Engine engine, RenderManager renderManager)
    {
        this.engine = engine;
        this.renderManager = renderManager;

        entitiesSharingRenderingArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<ShareRenderEnabledBit>()
            .WithComponentData<RenderEnabledBit>();

        dynamicParentedRenderersArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<CopyParentTransform>()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<RenderEnabledBit>();

        updateWorldBoundsArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<MeshBounds>();

        toCullArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<PerformCullingBit>()
            .WithComponentData<WorldMeshBounds>();

        toRenderArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<RenderEnabledBit>()
            .WithComponentData<LocalToWorld>()
            .WithComponentData<WorldMeshBounds>()
            .WithComponentData<MeshRenderer>();

        dirtMeshRenderersArchetype = engine.entityManager.NewArchetype()
            .WithComponentData<DirtyPosition>()
            .WithComponentData<MeshRenderer>()
            .WithComponentData<LocalToWorld>();
    }

    /// <summary>Max distance from the camera to the farthest bounds point of any collected shadow
    /// caster this frame; the cascade fit uses it to pull the light near plane back far enough that
    /// no collected caster is ever clipped (see CascadedShadowMapManager.ComputeCascades).</summary>
    internal float ShadowCasterReach => shadowCasterReach;

    /// <summary>The number of renderers collected for the game camera this frame; indices below this are valid picking results.</summary>
    internal int TotalToDraw => mainSet.totalToDraw;

    /// <summary>Maps a game-view draw index (as written by the shaders into the picking attachment) back to its entity.</summary>
    internal Entity EntityAtIndex(uint index) => mainSet.renderersData[index].Item2;

    // The scene view draws sceneSet when it culls independently, otherwise mainSet - picking from the
    // scene view must resolve through whichever set actually produced its image.
    private RenderSet SceneViewSet => sceneSetActive ? sceneSet : mainSet;

    /// <summary>The number of renderers the scene view drew this frame; indices below this are valid scene-view picking results.</summary>
    internal int SceneViewTotalToDraw => SceneViewSet.totalToDraw;

    /// <summary>Maps a scene-view draw index back to its entity.</summary>
    internal Entity SceneViewEntityAtIndex(uint index) => SceneViewSet.renderersData[index].Item2;

    public void PrepareFrame(ICamera camera)
    {
        // game camera owns IsCulled / WorldMeshBounds this frame; a later PrepareSceneFrame may
        // overwrite IsCulled for the scene camera, which is fine since mainSet is already collected.
        sceneSetActive = false;

        var cameraPosition = camera.Transform.Position;

        boundsUpdate.Restart();
        updateWorldBoundsArchetype.ParallelForEach<LocalToWorld, MeshBounds, WorldMeshBounds, DirtyPosition>((itr, thread, start, end, l2w, meshBounds, worldMeshBounds, dirtyBit) =>
        {
            Span<Vector3> corners = stackalloc Vector3[8];
            for (int i = start; i < end; ++i)
            {
                if (!dirtyBit[i])
                    continue;

                worldMeshBounds[i] = RenderManager.LocalToWorld(in meshBounds[i], in l2w[i], ref corners);
            }
        });
        boundsUpdate.Stop();
        engine.statsManager.Counters.BoundsCalc.Add(boundsUpdate.Elapsed.TotalMilliseconds);

        CollectInto(camera, mainSet);
        CollectShadowCasters(renderManager.LayersArray, cameraPosition);
    }

    /// <summary>
    /// Builds <see cref="sceneSet"/> by culling/collecting against the scene-view camera, so the scene
    /// view can render what ITS camera sees rather than the game camera's set. Must run after
    /// <see cref="PrepareFrame"/> (which refreshes WorldMeshBounds and collects the shadow casters):
    /// this only re-runs the per-camera cull + collect, reusing the bounds computed there.
    /// </summary>
    public void PrepareSceneFrame(ICamera camera)
    {
        CollectInto(camera, sceneSet);
        sceneSetActive = true;
    }

    // Culls every renderer against `camera`, then collects + sorts the survivors and builds the
    // per-frame instancing buffers into `set`. Assumes WorldMeshBounds are already current for the
    // frame (PrepareFrame updates them). Writes RenderEnabledBit.IsCulled for `camera`.
    private ICamera _cachedCamera;
    private BoundingFrustum _cachedFrustum;
    private RenderSet _cachedRenderSet;
    private int opaqueIndex = 0;
    private int transparentIndex = 0;
    private (int opaque, int transparent)[] count = new (int, int)[EntityExtensions.ParallelThreads];
    private void CollectInto(ICamera camera, RenderSet set)
    {
        _cachedCamera = camera;
        _cachedRenderSet = set;
        _cachedFrustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);

        culler.Restart();
        toCullArchetype.ParallelForEachState<ObjectDrawRenderStage, LocalToWorld, PerformCullingBit, RenderEnabledBit, WorldMeshBounds>(this, static
            (that, itr, thread, start, end, l2w, _ , bits, worldMeshBounds) =>
        {
            var cameraPosition = that._cachedCamera.Transform.Position;
            var layers = that.renderManager.LayersArray;
            float mod = that._cachedCamera.ViewDistanceModifier * that._cachedCamera.ViewDistanceModifier;

            for (int i = start; i < end; ++i)
            {
                if (bits[i].IsForceDisabled())
                    continue;

                if (bits[i].Layer > 0 && layers[bits[i].Layer].IsDisabled)
                    continue;

                ref var boundingBox = ref worldMeshBounds[i].box;
                var pos = boundingBox.Center;
                var boundingBoxSize = boundingBox.Size;
                var size = boundingBoxSize.X + boundingBoxSize.Y + boundingBoxSize.Z;
                size = MathF.Sqrt(Math.Max(size, 10));
                bool doRender = (pos - cameraPosition).LengthSquared() < (size) * (size) * (size) * mod;
                if (doRender)
                {
                    bits[i].IsCulled = that._cachedFrustum.Contains(ref boundingBox) == ContainmentType.Disjoint;
                }
                else
                    bits[i].IsCulled = true;
            }
        });
        dynamicParentedRenderersArchetype.ParallelForEachState<ObjectDrawRenderStage, RenderEnabledBit, CopyParentTransform>(this,
            static (that, itr, thread, start, end, renderBit, cpt) =>
        {
            RenderManager.CachedComponentDataAccess<RenderEnabledBit> cache = new(that.engine.entityManager);
            for (int i = start; i < end; ++i)
            {
                var parent = cpt[i];
                if (parent.Parent != Entity.Empty)
                {
                    var forceDisabled = renderBit[i].IsForceDisabled();
                    renderBit[i] = cache[parent.Parent];
                    if (forceDisabled)
                        renderBit[i].SetDisabled(true);
                }
            }
        });
        entitiesSharingRenderingArchetype.ParallelForEachState<ObjectDrawRenderStage, RenderEnabledBit, ShareRenderEnabledBit>(this,
            static (that, itr, thread, start, end, renderBits, shareRenderBits) =>
            {
                RenderManager.CachedComponentDataAccess<RenderEnabledBit> cache = new(that.engine.entityManager);
                for (int i = start; i < end; ++i)
                {
                    if (!renderBits[i] && shareRenderBits[i].OtherEntity != Entity.Empty)
                    {
                        renderBits[i] = cache[shareRenderBits[i].OtherEntity];
                    }
                }
            });
        culler.Stop();
        engine.statsManager.Counters.Culling.Add(culler.Elapsed.TotalMilliseconds);

        dirtMeshRenderersArchetype.ParallelForEachArrayState<ObjectDrawRenderStage, MeshRenderer, LocalToWorld, DirtyPosition>(this,
            static (that, itr, thread, start, end, renderers, l2w, dirty) =>
            {
                var meshManager = that.engine.meshManager;
                for (int i = start; i < end; ++i)
                {
                    if (!dirty[i])
                        continue;

                    ref var localToWorld = ref l2w[i];
                    var span = renderers[i];
                    for (int j = 0; j < span.Length; ++j)
                    {
                        var mesh = meshManager.GetMeshByHandle(span[j].MeshHandle);
                        var bounds = mesh.Bounds;
                        var worldBounds = RenderManager.LocalToWorld(new MeshBounds() { box = bounds }, in localToWorld);
                        span[j].WorldBounds = worldBounds;
                    }
                }
            });

        for (int i = 0; i < count.Length; ++i)
        {
            count[i] = default;
        }
        toRenderArchetype.ParallelForEachArrayState<ObjectDrawRenderStage, MeshRenderer, RenderEnabledBit, LocalToWorld>(this,
            static (that, itr, thread, start, end, renderers, renderBit, l2w) =>
        {
            var layers = that.renderManager.LayersArray;
            int opaque = 0;
            int transparent = 0;
            for (int i = start; i < end; ++i)
            {
                if (!renderBit[i])
                    continue;

                if (renderBit[i].Layer > 0 && layers[renderBit[i].Layer].IsDisabled)
                    continue;

                ref var localToWorld = ref l2w[i];

                var span = renderers[i];
                for (int j = 0; j < span.Length; ++j)
                {
                    if (span[j].Hidden)
                    {
                        span[j].SkipDraw = true;
                        continue;
                    }

                    if (span[j].WorldBounds.box == default)
                    {
                        var mesh = that.engine.meshManager.GetMeshByHandle(span[j].MeshHandle);
                        var bounds = mesh.Bounds;
                        span[j].WorldBounds = RenderManager.LocalToWorld(new MeshBounds() { box = bounds }, in localToWorld);
                    }

                    // Per-renderer frustum culling is only worth it for multi-renderer entities: the entity-level
                    // culling pass (PerformCullingBit) already tested the entity's WorldMeshBounds, and for a
                    // single renderer those bounds are the renderer's bounds, so re-testing here would be redundant.
                    // With multiple renderers the entity bounds are the union of submesh bounds, so individual
                    // submeshes can still be off-screen even when the entity is visible.
                    if (span.Length > 1)
                        span[j].SkipDraw = that._cachedFrustum.Contains(ref span[j].WorldBounds.box) == ContainmentType.Disjoint;
                    else
                        span[j].SkipDraw = false;

                    if (span[j].SkipDraw)
                        continue;

                    if (span[j].Opaque)
                        opaque++;
                    else
                        transparent++;
                }
            }
            that.count[thread] = (opaque + that.count[thread].opaque, transparent + that.count[thread].transparent);
        });
        set.opaque = 0;
        set.transparent = 0;
        for (int i = 0; i < count.Length; ++i)
        {
            set.opaque += count[i].opaque;
            set.transparent += count[i].transparent;
        }
        set.totalToDraw = set.opaque + set.transparent;
        if (set.renderersData.Length < set.totalToDraw)
        {
            set.renderersData = new (LocalToWorld, Entity)[set.totalToDraw];
            set.renderers = new MeshRenderer[set.totalToDraw];
        }
        opaqueIndex = 0;
        transparentIndex = set.opaque;
        toRenderArchetype.ForEachArrayState<ObjectDrawRenderStage, MeshRenderer, LocalToWorld, RenderEnabledBit>(this, static (that, itr, thread, start, end, meshRenderers, l2w, render) =>
        {
            var layers = that.renderManager.LayersArray;
            var set = that._cachedRenderSet;
            var localOpaqueIndex = that.opaqueIndex;
            var localTransparentIndex = that.transparentIndex;
            for (int i = start; i < end; ++i)
            {
                if (!render[i])
                    continue;

                if (render[i].Layer > 0 && layers[render[i].Layer].IsDisabled)
                    continue;

                var renderers_ = meshRenderers[i];

                for (int j = 0; j < renderers_.Length; ++j)
                {
                    ref var renderer = ref renderers_[j];
                    if (renderer.SkipDraw)
                        continue;
                    if (renderer.Opaque)
                    {
                        set.renderers[localOpaqueIndex] = renderer;
                        set.renderersData[localOpaqueIndex++] = (l2w[i], itr[i]);
                    }
                    else
                    {
                        set.renderers[localTransparentIndex] = renderer;
                        set.renderersData[localTransparentIndex++] = (l2w[i], itr[i]);
                    }
                }
            }

            that.opaqueIndex = localOpaqueIndex;
            that.transparentIndex = localTransparentIndex;
        });

        sorting.Restart();
        SortByKey(set.renderers, set.renderersData, 0, set.opaque);
        SortByKey(set.renderers, set.renderersData, set.opaque, set.transparent);
        sorting.Stop();
        engine.statsManager.Counters.Sorting.Add(sorting.Elapsed.TotalMilliseconds);

        set.globalBuffersReady = false;
        set.globalBuffersUploaded = false;
        if (set.totalToDraw > 0)
        {
            if (set.globalModelsArray.Length < set.totalToDraw)
            {
                set.globalModelsArray = new Matrix[set.totalToDraw];
                set.globalInverseModelsArray = new Matrix[set.totalToDraw];
                set.globalObjectIndicesArray = new uint[set.totalToDraw];
                set.globalDrawDataArray = new Int4[set.totalToDraw];
                set.globalMaterialIndexArray = new int[set.totalToDraw];
            }
            for (int idx = 0; idx < set.totalToDraw; ++idx)
            {
                set.globalModelsArray[idx] = set.renderersData[idx].Item1.Matrix;
                set.globalInverseModelsArray[idx] = set.renderersData[idx].Item1.Inverse;
                set.globalObjectIndicesArray[idx] = (uint)(idx + 1);
                set.globalDrawDataArray[idx] = set.renderers[idx].InstanceData ?? new Int4(-1, -1, -1, -1);
                set.globalMaterialIndexArray[idx] = engine.materialManager.GetMaterialByHandle(set.renderers[idx].MaterialHandle).MaterialArrayIndex;
            }
            set.globalBuffersReady = true;
        }
    }

    private partial struct FilterShadowsDistanceJob : IParallelJob
    {
        // set in auto generated Run method
        public IChunkDataIterator itr;
        public ComponentArrayDataAccess<MeshRenderer> meshRenderers;
        public ComponentDataAccess<LocalToWorld> localToWorld;
        public ComponentDataAccess<RenderEnabledBit> renderBit;

        // custom fields user want to use
        public Vector3 cameraPosition;
        public float shadowDist;
        public RenderLayerData[] layers;
        public int casterCount;

        public void Execute(int thread, int start, int end)
        {
            int c = 0;
            for (int i = start; i < end; ++i)
            {
                // deliberately NOT the implicit bool (which includes the camera cull written by
                // CollectInto): casters behind/beside the camera or distance-culled for the view must
                // still cast into the visible cascades. Only explicit hiding excludes a caster.
                if (renderBit[i].IsForceDisabled())
                    continue;
                if (renderBit[i].Layer > 0 && layers[renderBit[i].Layer].IsDisabled)
                    continue;
                var span = meshRenderers[i];
                for (int j = 0; j < span.Length; ++j)
                {
                    if (span[j].Hidden || !span[j].Opaque)
                        continue;
                    // closest-point test: a large object (city WMO) whose bounds CENTER is beyond the
                    // shadow distance can still overlap the shadowed range and cast into it.
                    ref readonly var box = ref span[j].WorldBounds.box;
                    float maxDist = shadowDist + box.Size.Length() * 0.5f;
                    if ((box.Center - cameraPosition).LengthSquared() > maxDist * maxDist)
                        continue;
                    c++;
                }
            }

            Interlocked.Add(ref casterCount, c);
        }
    }

    // Sequential (IJob, not IParallelJob): this appends into shadowCasters/shadowCastersData with a
    // shared running casterIndex, which is only correct single-threaded. As an IParallelJob the
    // non-atomic casterIndex++ raced across threads - casters overwrote each other and left gaps holding
    // stale matrices (flickering / wrong-place shadows), and the racy bounds check could write out of
    // range (the random GPU page fault). The "fill sequentially to keep a stable order for batching"
    // requirement also needs single-threaded execution.
    private partial struct FillShadowsJob : IJob
    {
        // set in auto generated Run method
        public IChunkDataIterator itr;
        public ComponentArrayDataAccess<MeshRenderer> meshRenderers;
        public ComponentDataAccess<LocalToWorld> localToWorld;
        public ComponentDataAccess<RenderEnabledBit> render;

        // custom fields user want to use
        public Vector3 cameraPosition;
        public float shadowDist;
        public RenderLayerData[] layers;
        public MeshRenderer[] shadowCasters;
        public (LocalToWorld, Entity)[] shadowCastersData;
        public int shadowCasterCount;
        public int casterIndex;
        public float casterReach; // max distance from the camera to any accepted caster's farthest bounds point

        public void Execute(int start, int end)
        {
            for (int i = start; i < end; ++i)
            {
                // matches FilterShadowsDistanceJob: no camera cull, only explicit hiding excludes a caster
                if (render[i].IsForceDisabled())
                    continue;
                if (render[i].Layer > 0 && layers[render[i].Layer].IsDisabled)
                    continue;
                var span = meshRenderers[i];
                for (int j = 0; j < span.Length; ++j)
                {
                    ref var renderer = ref span[j];
                    if (renderer.Hidden || !renderer.Opaque)
                        continue;
                    ref readonly var box = ref renderer.WorldBounds.box;
                    float radius = box.Size.Length() * 0.5f;
                    float dist = (box.Center - cameraPosition).Length();
                    if (dist - radius > shadowDist)
                        continue;
                    if (casterIndex >= shadowCasterCount)
                        continue; // a renderer toggled on between the count and fill passes - drop it
                    casterReach = MathF.Max(casterReach, dist + radius);
                    shadowCasters[casterIndex] = renderer;
                    shadowCastersData[casterIndex++] = (localToWorld[i], itr[i]);
                }
            }
        }
    }

    /// <summary>
    /// Collects the shadow-caster set: every enabled, non-hidden, opaque renderer whose bounds are
    /// within the shadow distance of the camera - WITHOUT the camera frustum cull, so objects behind
    /// or beside the camera (which still cast shadows into the visible cascades) are included. Builds
    /// the caster instancing buffers the same way the camera set does. The reused <see cref="MeshRenderer.WorldBounds"/>
    /// were already refreshed by the main collection pass above.
    /// </summary>
    private void CollectShadowCasters(RenderLayerData[] layers, Vector3 cameraPosition)
    {
        shadowBuffersReady = false;
        shadowBuffersUploaded = false;
        shadowCasterCount = 0;
        shadowCasterReach = 0;

        float shadowDist = renderManager.ShadowDistance;

        // count first (parallel), so the caster arrays can be sized once
        var preFilterJob = new FilterShadowsDistanceJob()
        {
            cameraPosition = cameraPosition,
            shadowDist = shadowDist,
            layers = layers,
        };
        preFilterJob.Run(toRenderArchetype);
        shadowCasterCount = preFilterJob.casterCount;
        if (shadowCasterCount == 0)
            return;

        if (shadowCasters.Length < shadowCasterCount)
        {
            shadowCasters = new MeshRenderer[shadowCasterCount];
            shadowCastersData = new (LocalToWorld, Entity)[shadowCasterCount];
        }

        // fill sequentially to keep a stable order for batching
        var job = new FillShadowsJob()
        {
            cameraPosition = cameraPosition,
            shadowDist = shadowDist,
            layers = layers,
            shadowCasters = shadowCasters,
            shadowCastersData = shadowCastersData,
            shadowCasterCount = shadowCasterCount,
            casterIndex = 0
        };
        job.Run(toRenderArchetype);
        shadowCasterCount = job.casterIndex;
        shadowCasterReach = job.casterReach;

        if (shadowCasterCount == 0)
            return;

        SortByKey(shadowCasters, shadowCastersData, 0, shadowCasterCount);

        if (shadowModelsArray.Length < shadowCasterCount)
        {
            shadowModelsArray = new Matrix[shadowCasterCount];
            shadowInverseModelsArray = new Matrix[shadowCasterCount];
            shadowObjectIndicesArray = new uint[shadowCasterCount];
            shadowDrawDataArray = new Int4[shadowCasterCount];
            shadowMaterialIndexArray = new int[shadowCasterCount];
        }
        for (int idx = 0; idx < shadowCasterCount; ++idx)
        {
            shadowModelsArray[idx] = shadowCastersData[idx].Item1.Matrix;
            shadowInverseModelsArray[idx] = shadowCastersData[idx].Item1.Inverse;
            shadowObjectIndicesArray[idx] = (uint)(idx + 1);
            shadowDrawDataArray[idx] = shadowCasters[idx].InstanceData ?? new Int4(-1, -1, -1, -1);
            shadowMaterialIndexArray[idx] = engine.materialManager.GetMaterialByHandle(shadowCasters[idx].MaterialHandle).MaterialArrayIndex;
        }
        shadowBuffersReady = true;
    }

    /// <summary>Uploads the global per-frame instancing buffers (built in <see cref="CollectInto"/>) once, on the first <see cref="Render"/> call that uses the set this frame.</summary>
    private void EnsureGlobalBuffersUploaded(EngineCommandList cl, RenderSet set)
    {
        if (set.globalBuffersUploaded || !set.globalBuffersReady)
            return;
        set.globalModelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)set.globalModelsArray.AsSpan(0, set.totalToDraw));
        set.globalInverseModelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)set.globalInverseModelsArray.AsSpan(0, set.totalToDraw));
        set.globalObjectIndicesBuffer = cl.UploadTransientBuffer((ReadOnlySpan<uint>)set.globalObjectIndicesArray.AsSpan(0, set.totalToDraw));
        set.globalDrawDataBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Int4>)set.globalDrawDataArray.AsSpan(0, set.totalToDraw));
        set.globalMaterialIndexBuffer = cl.UploadTransientBuffer((ReadOnlySpan<int>)set.globalMaterialIndexArray.AsSpan(0, set.totalToDraw));
        set.globalBuffersUploaded = true;
    }

    /// <summary>Uploads the shadow-caster instancing buffers once per frame, on the first shadow draw.</summary>
    private void EnsureShadowBuffersUploaded(EngineCommandList cl)
    {
        if (shadowBuffersUploaded || !shadowBuffersReady)
            return;
        shadowModelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)shadowModelsArray.AsSpan(0, shadowCasterCount));
        shadowInverseModelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)shadowInverseModelsArray.AsSpan(0, shadowCasterCount));
        shadowObjectIndicesBuffer = cl.UploadTransientBuffer((ReadOnlySpan<uint>)shadowObjectIndicesArray.AsSpan(0, shadowCasterCount));
        shadowDrawDataBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Int4>)shadowDrawDataArray.AsSpan(0, shadowCasterCount));
        shadowMaterialIndexBuffer = cl.UploadTransientBuffer((ReadOnlySpan<int>)shadowMaterialIndexArray.AsSpan(0, shadowCasterCount));
        shadowBuffersUploaded = true;
    }

    // Scratch buffers for the radix sort, reused across frames and sets (sorting is single-threaded
    // here, so sharing them is safe). Grown to the largest range sorted.
    private ulong[] sortKeysA = new ulong[1];
    private ulong[] sortKeysB = new ulong[1];
    private int[] sortIndicesA = new int[1];
    private int[] sortIndicesB = new int[1];
    private MeshRenderer[] sortRenderersTmp = new MeshRenderer[1];
    private (LocalToWorld, Entity)[] sortDataTmp = new (LocalToWorld, Entity)[1];

    /// <summary>
    /// Sorts <paramref name="renderers"/> and the parallel <paramref name="renderersData"/> over
    /// [start, start+count) by the precomputed <see cref="MeshRenderer.SortKey"/> using an LSD radix
    /// sort. The key already encodes opaque>shader>pipeline>material>mesh>submesh, so a plain ascending
    /// sort of the 64-bit key yields the batchable draw order. Only 8-byte keys + 4-byte indices are
    /// shuffled per pass; the heavy MeshRenderer / LocalToWorld payloads move exactly once (final gather).
    /// </summary>
    private void SortByKey(MeshRenderer[] renderers, (LocalToWorld, Entity)[] renderersData, int start, int count)
    {
        if (count <= 1)
            return;

        if (sortKeysA.Length < count)
        {
            sortKeysA = new ulong[count];
            sortKeysB = new ulong[count];
            sortIndicesA = new int[count];
            sortIndicesB = new int[count];
            sortRenderersTmp = new MeshRenderer[count];
            sortDataTmp = new (LocalToWorld, Entity)[count];
        }

        var keys = sortKeysA;
        var idx = sortIndicesA;
        for (int i = 0; i < count; ++i)
        {
            keys[i] = renderers[start + i].SortKey.Value;
            idx[i] = i;
        }

        // LSD radix over 8 bytes, ping-ponging between A and B. Bytes whose digit is uniform across
        // all keys (e.g. the opaque bit, which is constant within an opaque- or transparent-only range)
        // are skipped without a scatter, so high bytes are essentially free.
        var keysSrc = sortKeysA;
        var keysDst = sortKeysB;
        var idxSrc = sortIndicesA;
        var idxDst = sortIndicesB;
        Span<int> hist = stackalloc int[256];
        for (int shift = 0; shift < 64; shift += 8)
        {
            hist.Clear();
            for (int i = 0; i < count; ++i)
                hist[(int)((keysSrc[i] >> shift) & 0xFF)]++;

            // all keys share this digit -> already ordered for this pass, skip the scatter
            if (hist[(int)((keysSrc[0] >> shift) & 0xFF)] == count)
                continue;

            int sum = 0;
            for (int b = 0; b < 256; ++b)
            {
                int c = hist[b];
                hist[b] = sum;
                sum += c;
            }

            for (int i = 0; i < count; ++i)
            {
                int b = (int)((keysSrc[i] >> shift) & 0xFF);
                int dst = hist[b]++;
                keysDst[dst] = keysSrc[i];
                idxDst[dst] = idxSrc[i];
            }

            (keysSrc, keysDst) = (keysDst, keysSrc);
            (idxSrc, idxDst) = (idxDst, idxSrc);
        }

        // idxSrc now holds the sorted permutation (over [0, count)); gather payloads once, then copy back.
        for (int i = 0; i < count; ++i)
        {
            int src = start + idxSrc[i];
            sortRenderersTmp[i] = renderers[src];
            sortDataTmp[i] = renderersData[src];
        }
        Array.Copy(sortRenderersTmp, 0, renderers, start, count);
        Array.Copy(sortDataTmp, 0, renderersData, start, count);
    }

    private RenderSet SelectSet(ICamera camera)
        => sceneSetActive && ReferenceEquals(camera, engine.cameraManger.SceneViewCamera) ? sceneSet : mainSet;

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        // Cascaded shadow map: draw the dedicated camera-frustum-independent caster set (see
        // CollectShadowCasters) into the cascade depth target. The orchestrator has swapped
        // SceneData's view/projection for the light's ortho matrices. Reuses the Depth pass.
        if (point == RenderPoint.Shadow)
        {
            EnsureShadowBuffersUploaded(commandList);
            RenderRange(commandList, ShaderPassType.Depth, shadowCasters, shadowCastersData, 0, shadowCasterCount, false,
                shadowModelsBuffer, shadowInverseModelsBuffer, shadowObjectIndicesBuffer, shadowDrawDataBuffer, shadowMaterialIndexBuffer);
            return;
        }

        var set = SelectSet(camera);
        EnsureGlobalBuffersUploaded(commandList, set);
        if (point == RenderPoint.Opaque)
        {
            // these exact opaque objects were just rendered into the depth prepass, so their final
            // depth is already in the buffer. Shade each visible pixel exactly once with Equal +
            // depth-write-off (no redundant writes; write-off also keeps early-Z on for the cutout
            // shaders that discard, which Metal would otherwise force to late-Z). gl_Position is
            // declared invariant so the prepass and forward depths match exactly.
            commandList.Native.SetDepthStateOverride(Veldrid.ComparisonKind.Equal, false);
            RenderRange(commandList, ShaderPassType.Forward, set.renderers, set.renderersData, 0, set.opaque, false,
                set.globalModelsBuffer, set.globalInverseModelsBuffer, set.globalObjectIndicesBuffer, set.globalDrawDataBuffer, set.globalMaterialIndexBuffer);
            commandList.Native.SetDepthStateOverride(null, null);
        }
        else if (point == RenderPoint.Transparent)
            RenderRange(commandList, ShaderPassType.Forward, set.renderers, set.renderersData, set.opaque, set.totalToDraw, true,
                set.globalModelsBuffer, set.globalInverseModelsBuffer, set.globalObjectIndicesBuffer, set.globalDrawDataBuffer, set.globalMaterialIndexBuffer);
        else if (point == RenderPoint.DepthPrepass)
            RenderRange(commandList, ShaderPassType.Depth, set.renderers, set.renderersData, 0, set.opaque, false,
                set.globalModelsBuffer, set.globalInverseModelsBuffer, set.globalObjectIndicesBuffer, set.globalDrawDataBuffer, set.globalMaterialIndexBuffer);
    }

    // Every batch in a range shares the same per-frame instancing SSBOs (built in CollectInto and
    // uploaded once via EnsureGlobalBuffersUploaded); each batch just offsets into them via
    // firstInstance. The range is only entered with renderers present, which guarantees the buffers
    // were built and uploaded - an empty range returns immediately.
    private void RenderRange(EngineCommandList cl, ShaderPassType shaderPassType,
        MeshRenderer[] renderers, (LocalToWorld, Entity)[] renderersData, int start, int end, bool transparent,
        INativeBuffer? modelsBuffer, INativeBuffer? inverseModelsBuffer,
        INativeBuffer? objectIndicesBuffer, INativeBuffer? drawDataBuffer, INativeBuffer? materialIndexBuffer)
    {
        if (start >= end)
            return;

        cl.InsertDebugMarker(transparent ? "  Rendering translucent" : "  Rendering opaque");
        drawing.Restart();
        // The per-frame instancing SSBOs are identical for every batch in this range, so bind them
        // once as persistent buffers instead of re-queuing all five per batch (each SetMaterial then
        // resolves them without the five SetBuffer calls). Cleared at the end of the range so they
        // never leak into the next range or other stages (ImGui, post).
        cl.SetPersistentBuffer(ShaderUniforms.InstancingModels, modelsBuffer!);
        cl.SetPersistentBuffer(ShaderUniforms.InstancingInverseModels, inverseModelsBuffer!);
        cl.SetPersistentBuffer(ShaderUniforms.InstancingObjectIndices, objectIndicesBuffer!);
        cl.SetPersistentBuffer(ShaderUniforms.InstancingDrawData, drawDataBuffer!);
        cl.SetPersistentBuffer(ShaderUniforms.InstancingMaterialIndex, materialIndexBuffer!);

        // Nothing is per-draw in the bindless / global-buffer path: every object reads its transform,
        // per-object ints and material row through the set-1 instancing SSBOs (bound once above) and
        // set-3 globals, indexed by gl_InstanceIndex; textures are bindless. So set 1 (= the MaterialData
        // SSBO + those instancing buffers) is CONSTANT within a shader. We therefore bind set 1 only when
        // the shader changes, switch just the pipeline on a variant change, and otherwise go straight
        // from mesh bind to draw. The list is sorted shader>pipeline>mesh>submesh, so these are runs.
        IShaderPass? lastPass = null;
        global::TheEngine.Resources.Pipeline? lastPipeline = null;
        for (int i = start; i < end; ++i)
        {
            var mr = renderers[i];
            var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
            var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);

            var shaderInstanced = material.GetShaderPass(shaderPassType);
            var meshId = mr.SubMeshId;

            var toBatch = 0;
            {
                int j = i + 1;
                while (j < end && renderers[j].MeshHandle == mr.MeshHandle &&
                       renderers[j].SubMeshId == mr.SubMeshId &&
                       renderers[j].MaterialHandle == mr.MaterialHandle)
                {
                    toBatch++;
                    j++;
                }
            }

            // A renderer whose material was never registered keeps MaterialArrayIndex == -1 (e.g. a model
            // with 0 materials such as a particle-emitter doodad). The shader indexes materials[matIndex],
            // so -1 is a negative out-of-bounds SSBO read = GPU page fault on MoltenVK. Skip the whole
            // batch (the shader also clamps as a backstop). i += toBatch keeps the outer loop's stride.
            if (material.MaterialArrayIndex < 0)
            {
                i += toBatch;
                continue;
            }

            if (!ReferenceEquals(shaderInstanced, lastPass))
            {
                // shader changed: (re)bind the pipeline AND set 1 (MaterialData SSBO + instancing)
                cl.SetMaterial(material, shaderInstanced!);
                lastPass = shaderInstanced;
                lastPipeline = material.Pipeline;
            }
            else if (!ReferenceEquals(material.Pipeline, lastPipeline))
            {
                // same shader, different pipeline variant: switch pipeline only. Set 1 is unchanged
                // (a shader has one MaterialData layout/buffer), so it stays bound from above.
                cl.SetPipeline(material, shaderInstanced!);
                lastPipeline = material.Pipeline;
            }

            // this batch's slice starts at firstInstance = i, and gl_InstanceIndex = firstInstance +
            // gl_InstanceID addresses the instancing SSBOs directly. DrawIndexedInstanced binds the
            // mesh's vertex/index buffers only when the mesh actually changes, so a run reduces to
            // "switch mesh -> draw".
            cl.DrawIndexedInstanced(mesh, meshId, toBatch + 1, firstInstance: i);

            i += toBatch;
        }
        cl.ClearPersistentBuffers();
        drawing.Stop();
        engine.statsManager.Counters.Drawing.Add(drawing.Elapsed.TotalMilliseconds);
    }

    public void EndFrame()
    {
        // renderersData is intentionally kept after the frame: object picking reads back a draw
        // index from the picking attachment and resolves it through EntityAtIndex later.
    }

    public void Dispose()
    {
    }
}
