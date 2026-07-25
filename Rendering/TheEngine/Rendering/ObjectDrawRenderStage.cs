using System.Diagnostics;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// The built-in stage that draws all ECS mesh-renderer entities: culls and sorts them
/// in <see cref="PrepareFrame"/>, then records the opaque and transparent ranges at the
/// respective render points, batching consecutive identical draws into instanced draws.
/// Also keeps the draw-index-to-entity mapping that object picking reads back.
/// </summary>
internal sealed class ObjectDrawRenderStage : IRenderStage
{
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

    private readonly MaterialInstanceRenderData instancingRenderData = new();
    private Matrix[] instancesArray = new Matrix[1];
    private Matrix[] inverseInstancesArray = new Matrix[1];
    private uint[] instancesObjectIndicesArray = new uint[1];
    private Int4[] instancesObjectDataArray = new Int4[1];

    // Note: per-instance shader data (Int4) is read from MeshRenderer.InstanceData directly during rendering;
    // the MaterialInstanceRenderData here only carries per-entity buffer bindings (e.g. bone matrices).
    private (LocalToWorld, MaterialInstanceRenderData?, Entity)[] renderersData = new (LocalToWorld, MaterialInstanceRenderData?, Entity)[10000];
    private MeshRenderer[] renderers = new MeshRenderer[10000];
    private int opaque;
    private int transparent;
    private int totalToDraw;

    private bool enableInstancing = true;

    public string Name => "Objects";

    public RenderPoint RenderPoints => RenderPoint.Opaque | RenderPoint.Transparent;

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

    /// <summary>The number of renderers collected for the current frame; indices below this are valid picking results.</summary>
    internal int TotalToDraw => totalToDraw;

    /// <summary>Maps a draw index (as written by the shaders into the picking attachment) back to its entity.</summary>
    internal Entity EntityAtIndex(uint index) => renderersData[index].Item3;

    public void PrepareFrame(ICamera camera)
    {
        var cameraPosition = camera.Transform.Position;
        var frustum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
        var entityManager = engine.EntityManager;
        var layers = renderManager.LayersArray;

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

        culler.Restart();
        float mod = renderManager.ViewDistanceModifier * renderManager.ViewDistanceModifier;
        toCullArchetype.ParallelForEach<LocalToWorld, PerformCullingBit, RenderEnabledBit, WorldMeshBounds>((itr, thread, start, end, l2w, _ , bits, worldMeshBounds) =>
        {
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
                    bits[i].IsCulled = frustum.Contains(ref boundingBox) == ContainmentType.Disjoint;
                }
                else
                    bits[i].IsCulled = true;
            }
        });
        dynamicParentedRenderersArchetype.ParallelForEach<RenderEnabledBit, CopyParentTransform>((itr, thread, start, end, renderBit, cpt) =>
        {
            RenderManager.CachedComponentDataAccess<RenderEnabledBit> cache = new(entityManager);
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
        entitiesSharingRenderingArchetype.ParallelForEach<RenderEnabledBit, ShareRenderEnabledBit>(
            (itr, thread, start, end, renderBits, shareRenderBits) =>
            {
                RenderManager.CachedComponentDataAccess<RenderEnabledBit> cache = new(entityManager);
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

        var meshManager = engine.meshManager;
        dirtMeshRenderersArchetype.ParallelForEachArray<MeshRenderer, LocalToWorld, DirtyPosition>((itr, thread, start, end, renderers, l2w, dirty) =>
            {
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

        ThreadLocal<(int opaque, int transparent)> count = new(true);
        toRenderArchetype.ParallelForEachArray<MeshRenderer, RenderEnabledBit, LocalToWorld>((itr, thread, start, end, renderers, renderBit, l2w) =>
        {
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
                        var mesh = meshManager.GetMeshByHandle(span[j].MeshHandle);
                        var bounds = mesh.Bounds;
                        span[j].WorldBounds = RenderManager.LocalToWorld(new MeshBounds() { box = bounds }, in localToWorld);
                    }

                    // Per-renderer frustum culling is only worth it for multi-renderer entities: the entity-level
                    // culling pass (PerformCullingBit) already tested the entity's WorldMeshBounds, and for a
                    // single renderer those bounds are the renderer's bounds, so re-testing here would be redundant.
                    // With multiple renderers the entity bounds are the union of submesh bounds, so individual
                    // submeshes can still be off-screen even when the entity is visible.
                    if (span.Length > 1)
                        span[j].SkipDraw = frustum.Contains(ref span[j].WorldBounds.box) == ContainmentType.Disjoint;
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
            if (count.IsValueCreated)
                count.Value = (opaque + count.Value.opaque, transparent + count.Value.transparent);
            else
                count.Value = (opaque, transparent);
        });
        opaque = count.Values.Sum(i => i.opaque);
        transparent = count.Values.Sum(i => i.transparent);
        totalToDraw = opaque + transparent;
        if (renderersData.Length < totalToDraw)
        {
            renderersData = new (LocalToWorld, MaterialInstanceRenderData?, Entity)[totalToDraw];
            renderers = new MeshRenderer[totalToDraw];
        }
        int opaqueIndex = 0;
        int transparentIndex = opaque;
        toRenderArchetype.ForEachRRROArray<MeshRenderer, LocalToWorld, RenderEnabledBit, MaterialInstanceRenderData>((itr, thread, start, end, meshRenderers, l2w, render, materialData) =>
        {
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
                        renderers[opaqueIndex] = renderer;
                        renderersData[opaqueIndex++] = (l2w[i], materialData?[i], itr[i]);
                    }
                    else
                    {
                        renderers[transparentIndex] = renderer;
                        renderersData[transparentIndex++] = (l2w[i], materialData?[i], itr[i]);
                    }
                }
            }
        });

        sorting.Restart();
        SortRenderersByMesh(0, opaque);
        SortRenderersByMesh(opaque + 1, totalToDraw);
        sorting.Stop();
        engine.statsManager.Counters.Sorting.Add(sorting.Elapsed.TotalMilliseconds);
    }

    private void SortRenderersByMesh(int start, int end)
    {
        if (end <= start)
            return;
        Array.Sort(renderers, renderersData, start, end - start, Comparer<MeshRenderer>.Create((a, b) =>
        {
            if (a.PipelineHandle != b.PipelineHandle)
            {
                return a.PipelineHandle.Handle.CompareTo(b.PipelineHandle.Handle);
            }
            if (a.MeshHandle == b.MeshHandle)
            {
                if (a.SubMeshId == b.SubMeshId)
                    return a.MaterialHandle.Handle.CompareTo(b.MaterialHandle.Handle);
                return a.SubMeshId.CompareTo(b.SubMeshId);
            }
            return a.MeshHandle.Handle.CompareTo(b.MeshHandle.Handle);
        }));
    }

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        if (point == RenderPoint.Opaque)
            RenderRange(commandList, ShaderPassType.Forward, 0, opaque, false);
        else if (point == RenderPoint.Transparent)
            RenderRange(commandList, ShaderPassType.Forward, opaque, totalToDraw, true);
    }

    private void RenderRange(EngineCommandList cl, ShaderPassType shaderPassType, int start, int end, bool transparent)
    {
        cl.InsertDebugMarker(transparent ? "  Rendering translucent" : "  Rendering opaque");
        drawing.Restart();
        for (int i = start; i < end; ++i)
        {
            var mr = renderers[i];
            var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
            var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);

            var shader = material.GetShaderPass(shaderPassType, false);
            var shaderInstanced = material.GetShaderPass(shaderPassType, true);
            var meshId = mr.SubMeshId;

            var toBatch = 0;
            if (shaderInstanced != null && // shader supports instancing
                enableInstancing &&                 // instancing is enabled
                renderersData[i].Item2 == null)     // no material per instance data
            {
                int j = i + 1;
                while (j < end && renderers[j].MeshHandle == mr.MeshHandle &&
                       renderers[j].SubMeshId == mr.SubMeshId &&
                       renderers[j].MaterialHandle == mr.MaterialHandle &&
                       renderersData[j].Item2 == null)
                {
                    toBatch++;
                    j++;
                }
            }

            if (toBatch <= 2)
            {
                cl.SetMaterial(material, shader!, renderersData[i].Item2);
                cl.SetObjectData(renderersData[i].Item1.Matrix, renderersData[i].Item1.Inverse, (uint)i + 1, mr.InstanceData);
                cl.DrawIndexed(mesh, meshId);
            }
            else
            {
                if (renderers[i + toBatch].MaterialHandle != renderers[i].MaterialHandle)
                {
                    Console.WriteLine(" no zjebane xd");
                }

                if (instancesArray.Length < toBatch + 1)
                {
                    instancesArray = new Matrix[toBatch + 1];
                    inverseInstancesArray = new Matrix[toBatch + 1];
                    instancesObjectIndicesArray = new uint[toBatch + 1];
                    instancesObjectDataArray = new Int4[toBatch + 1];
                }

                for (int k = 0; k < toBatch + 1; ++k)
                {
                    instancesArray[k] = renderersData[i + k].Item1.Matrix;
                    inverseInstancesArray[k] = renderersData[i + k].Item1.Inverse;
                    instancesObjectIndicesArray[k] = (uint)(i + k + 1);
                    instancesObjectDataArray[k] = renderers[i + k].InstanceData ?? new Int4(-1, -1, -1, -1);
                }

                var instancesBuffer = cl.UploadTransientBuffer(BufferInternalFormat.Float4, (ReadOnlySpan<Matrix>)instancesArray.AsSpan(0, toBatch + 1));
                var instancesInverseBuffer = cl.UploadTransientBuffer(BufferInternalFormat.Float4, (ReadOnlySpan<Matrix>)inverseInstancesArray.AsSpan(0, toBatch + 1));
                var instancesObjectIndicesBuffer = cl.UploadTransientBuffer(BufferInternalFormat.UInt, (ReadOnlySpan<uint>)instancesObjectIndicesArray.AsSpan(0, toBatch + 1));
                var instancesObjectDataBuffer = cl.UploadTransientBuffer(BufferInternalFormat.Int4, (ReadOnlySpan<Int4>)instancesObjectDataArray.AsSpan(0, toBatch + 1));

                instancingRenderData.Clear();
                instancingRenderData.SetBuffer("InstancingModels", instancesBuffer);
                instancingRenderData.SetBuffer("InstancingInverseModels", instancesInverseBuffer);
                instancingRenderData.SetBuffer("ObjectIndices", instancesObjectIndicesBuffer);
                instancingRenderData.SetBuffer("DrawData", instancesObjectDataBuffer);

                cl.SetMaterial(material, shaderInstanced!, instancingRenderData);
                cl.DrawIndexedInstanced(mesh, meshId, toBatch + 1);

                i += toBatch;
            }
        }
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
