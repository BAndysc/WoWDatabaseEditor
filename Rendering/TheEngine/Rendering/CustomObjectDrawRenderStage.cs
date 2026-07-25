using System;
using System.Collections.Generic;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Managers;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// Draws the "render once" requests scheduled via <see cref="Interfaces.IRenderManager.RenderOnce"/>
/// during Update (terrain gap fill, sky, gizmos, area triggers - things that are not persistent ECS
/// renderers). These used to record straight into the command list per call via the immediate
/// <c>Render(...)</c> path, each building its own transient instancing buffers and so missing the
/// set=1 descriptor cache. Here they are collected once per frame, sorted by pipeline/mesh/submesh/
/// material and drawn with the same global-instancing-buffer + baseInstance batching as the ECS
/// <see cref="ObjectDrawRenderStage"/>, so a run of same-material draws keeps set=1 bound once.
///
/// Like the ECS stage, the per-frame buffers are built in <see cref="PrepareFrame"/> (CPU only) and
/// uploaded once on the first <see cref="Render"/>; <c>gl_InstanceIndex = firstInstance + gl_InstanceID</c>
/// addresses each draw's slice directly. These draws are not pickable (object index 0).
/// </summary>
internal sealed class CustomObjectDrawRenderStage : IRenderStage
{
    private readonly Engine engine;
    // the same list instance RenderManager.RenderOnce appends to; cleared by RenderManager each frame
    private readonly List<(LocalToWorld, MeshRenderer)> renderers;

    // opaque draws occupy [0, opaque), transparent [opaque, total) - one contiguous range so a single
    // global buffer covers both and firstInstance offsets address it directly.
    private MeshRenderer[] sortedRenderers = new MeshRenderer[256];
    private LocalToWorld[] sortedData = new LocalToWorld[256];
    private int opaque;
    private int total;

    private Matrix[] modelsArray = new Matrix[256];
    private Matrix[] inverseModelsArray = new Matrix[256];
    private Int4[] drawDataArray = new Int4[256];
    private int[] materialIndexArray = new int[256];
    private uint[] objectIndicesArray = new uint[256];

    private bool buffersReady;
    private bool buffersUploaded;
    private INativeBuffer? modelsBuffer;
    private INativeBuffer? inverseModelsBuffer;
    private INativeBuffer? drawDataBuffer;
    private INativeBuffer? materialIndexBuffer;
    private INativeBuffer? objectIndicesBuffer;

    public string Name => "RenderOnce";

    public RenderPoint RenderPoints => RenderPoint.Opaque | RenderPoint.Transparent;

    internal CustomObjectDrawRenderStage(Engine engine, List<(LocalToWorld, MeshRenderer)> renderers)
    {
        this.engine = engine;
        this.renderers = renderers;
    }

    private static int Compare(in MeshRenderer a, in MeshRenderer b)
    {
        // primary key: the pipeline sort key (shader id in the top 8 bits), so all draws of one
        // shader - across its pipeline variants - are contiguous, minimising shader/set-1 switches.
        if (a.PipelineHandle != b.PipelineHandle)
        {
            var ka = a.PipelineHandle.SortKey;
            var kb = b.PipelineHandle.SortKey;
            if (ka != kb)
                return ka.CompareTo(kb);
        }
        if (a.MeshHandle != b.MeshHandle)
            return a.MeshHandle.Handle.CompareTo(b.MeshHandle.Handle);
        if (a.SubMeshId != b.SubMeshId)
            return a.SubMeshId.CompareTo(b.SubMeshId);
        return a.MaterialHandle.Handle.CompareTo(b.MaterialHandle.Handle);
    }

    private Comparer<MeshRenderer> meshComparer = Comparer<MeshRenderer>.Create((a, b) => Compare(in a, in b));

    public void PrepareFrame(ICamera camera)
    {
        opaque = 0;
        total = 0;
        buffersReady = false;
        buffersUploaded = false;

        int count = renderers.Count;
        if (count == 0)
            return;

        if (sortedRenderers.Length < count)
        {
            Array.Resize(ref sortedRenderers, count);
            Array.Resize(ref sortedData, count);
        }

        // partition: opaque first, transparent after, so [0, opaque) and [opaque, total) are contiguous
        for (int i = 0; i < count; ++i)
        {
            if (renderers[i].Item2.Opaque)
            {
                sortedRenderers[opaque] = renderers[i].Item2;
                sortedData[opaque] = renderers[i].Item1;
                opaque++;
            }
        }
        total = opaque;
        for (int i = 0; i < count; ++i)
        {
            if (!renderers[i].Item2.Opaque)
            {
                sortedRenderers[total] = renderers[i].Item2;
                sortedData[total] = renderers[i].Item1;
                total++;
            }
        }

        if (opaque > 0)
            Array.Sort(sortedRenderers, sortedData, 0, opaque, meshComparer);
        if (total > opaque)
            Array.Sort(sortedRenderers, sortedData, opaque, total - opaque, meshComparer);

        if (modelsArray.Length < total)
        {
            modelsArray = new Matrix[total];
            inverseModelsArray = new Matrix[total];
            drawDataArray = new Int4[total];
            materialIndexArray = new int[total];
            objectIndicesArray = new uint[total];
        }
        for (int idx = 0; idx < total; ++idx)
        {
            modelsArray[idx] = sortedData[idx].Matrix;
            inverseModelsArray[idx] = sortedData[idx].Inverse;
            drawDataArray[idx] = sortedRenderers[idx].InstanceData ?? new Int4(-1, -1, -1, -1);
            materialIndexArray[idx] = engine.materialManager.GetMaterialByHandle(sortedRenderers[idx].MaterialHandle).MaterialArrayIndex;
            objectIndicesArray[idx] = 0; // "render once" draws are not pickable
        }
        buffersReady = true;
    }

    private void EnsureBuffersUploaded(EngineCommandList cl)
    {
        if (buffersUploaded || !buffersReady)
            return;
        modelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)modelsArray.AsSpan(0, total));
        inverseModelsBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Matrix>)inverseModelsArray.AsSpan(0, total));
        objectIndicesBuffer = cl.UploadTransientBuffer((ReadOnlySpan<uint>)objectIndicesArray.AsSpan(0, total));
        drawDataBuffer = cl.UploadTransientBuffer((ReadOnlySpan<Int4>)drawDataArray.AsSpan(0, total));
        materialIndexBuffer = cl.UploadTransientBuffer((ReadOnlySpan<int>)materialIndexArray.AsSpan(0, total));
        buffersUploaded = true;
    }

    public void Render(RenderPoint point, EngineCommandList commandList, ICamera camera)
    {
        if (!buffersReady)
            return;
        EnsureBuffersUploaded(commandList);
        if (point == RenderPoint.Opaque)
            RenderRange(commandList, 0, opaque);
        else if (point == RenderPoint.Transparent)
            RenderRange(commandList, opaque, total);
    }

    private void RenderRange(EngineCommandList cl, int start, int end)
    {
        for (int i = start; i < end;)
        {
            var mr = sortedRenderers[i];
            var material = engine.materialManager.GetMaterialByHandle(mr.MaterialHandle);
            var mesh = engine.meshManager.GetMeshByHandle(mr.MeshHandle);
            var shaderPass = material.GetShaderPass(ShaderPassType.Forward);
            var meshId = mr.SubMeshId;

            // batch the contiguous run sharing mesh/submesh/material into one instanced draw
            int toBatch = 0;
            int j = i + 1;
            while (j < end && sortedRenderers[j].MeshHandle == mr.MeshHandle &&
                   sortedRenderers[j].SubMeshId == mr.SubMeshId &&
                   sortedRenderers[j].MaterialHandle == mr.MaterialHandle)
            {
                toBatch++;
                j++;
            }

            // the global buffers (built in PrepareFrame, uploaded once) cover [0, total); this batch's
            // slice starts at firstInstance = i, addressed by gl_InstanceIndex = i + gl_InstanceID.
            cl.SetBuffer(ShaderUniforms.InstancingModels, modelsBuffer!);
            cl.SetBuffer(ShaderUniforms.InstancingInverseModels, inverseModelsBuffer!);
            cl.SetBuffer(ShaderUniforms.InstancingObjectIndices, objectIndicesBuffer!);
            cl.SetBuffer(ShaderUniforms.InstancingDrawData, drawDataBuffer!);
            cl.SetBuffer(ShaderUniforms.InstancingMaterialIndex, materialIndexBuffer!);

            cl.SetMaterial(material, shaderPass!);
            cl.DrawIndexedInstanced(mesh, meshId, toBatch + 1, i);

            i += toBatch + 1;
        }
    }

    public void EndFrame()
    {
    }

    public void Dispose()
    {
    }
}
