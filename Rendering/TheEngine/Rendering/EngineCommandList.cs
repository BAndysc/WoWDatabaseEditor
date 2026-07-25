using System.Diagnostics;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// The engine-level recording API handed to render stages. A thin convenience layer
/// over <see cref="ICommandList"/> - it owns engine policy (material activation,
/// the per-draw object uniform, draw statistics) while the underlying command list
/// stays a pure, Vulkan-shaped command surface. It never bypasses the command list,
/// so when recording becomes deferred, stages keep working unchanged.
/// </summary>
public sealed class EngineCommandList
{
    private readonly ICommandList commandList;
    private RenderStats stats;

    private Matrix[] singleModels = new Matrix[1];
    private Matrix[] singleInverseModels = new Matrix[1];
    private uint[] singleObjectIndices = new uint[1];
    private Int4[] singleDrawData = new Int4[1];
    private int[] singleMaterialIndices = new int[1];

    internal EngineCommandList(ICommandList commandList)
    {
        this.commandList = commandList;
    }

    /// <summary>
    /// The raw command list, for engine-internal recording that has no engine-level
    /// equivalent yet (pass management, barriers, vertex streaming).
    /// </summary>
    internal ICommandList Native => commandList;

    internal ref RenderStats Stats => ref stats;

    internal void ResetStats() => stats = default;

    /// <summary>Binds the pipeline state, shader pass and the material's resources. Any buffers
    /// queued via <see cref="SetBuffer"/> since the last bind are consumed here.</summary>
    public void SetMaterial(Material material, IShaderPass shaderPass)
    {
        commandList.SetPipeline(material.Pipeline, shaderPass);
        commandList.BindMaterialResources(material);
        stats.MaterialActivations++;
    }

    public void SetMaterial(Material material, ShaderPassType shaderPassType)
        => SetMaterial(material, material.GetShaderPass(shaderPassType)!);

    /// <summary>Binds ONLY the pipeline state + shader pass, without (re)binding the material's
    /// set-1 resources. Use across a run of same-shader draws that only switch pipeline variants:
    /// set 1 (the MaterialData SSBO + the instancing buffers) is identical within a shader, so it is
    /// bound once via <see cref="SetMaterial"/> for the establishing draw and left in place.</summary>
    public void SetPipeline(Material material, IShaderPass shaderPass)
        => commandList.SetPipeline(material.Pipeline, shaderPass);

    /// <summary>Queues a named structured-buffer binding for the next <see cref="SetMaterial"/> call.</summary>
    public void SetBuffer(string name, INativeBuffer buffer)
        => commandList.SetBuffer(Material.GetUniformLocation(name), buffer);

    /// <summary>Hot-path overload taking a cached <see cref="GlobalUniformHandle"/> (see <see cref="ShaderUniforms"/>).</summary>
    public void SetBuffer(GlobalUniformHandle uniform, INativeBuffer buffer)
        => commandList.SetBuffer(uniform, buffer);

    /// <summary>Sets a buffer binding that persists across SetMaterial calls
    /// (see <see cref="ICommandList.SetPersistentBuffer"/>); pair with <see cref="ClearPersistentBuffers"/>.</summary>
    public void SetPersistentBuffer(GlobalUniformHandle uniform, INativeBuffer buffer)
        => commandList.SetPersistentBuffer(uniform, buffer);

    /// <summary>Clears all persistent buffer bindings set via <see cref="SetPersistentBuffer"/>.</summary>
    public void ClearPersistentBuffers()
        => commandList.ClearPersistentBuffers();

    /// <summary>
    /// Uploads <paramref name="count"/> identical copies of the per-object data (world matrix,
    /// picking index, per-draw ints, material index) into the Instancing SSBOs and queues them
    /// via <see cref="SetBuffer"/>. Every draw - even a single, non-batched one - reads
    /// its data through these SSBOs via gl_InstanceIndex, so this replaces the old per-draw
    /// ObjectData UBO entirely. <paramref name="count"/> &gt; 1 is for draws that share one
    /// transform across multiple instances (e.g. UIManager's glyph batches).
    /// </summary>
    public void PrepareInstancingData(Material material, in Matrix localToWorld, in Matrix worldToLocal,
        uint objectIndex = 0, Int4? drawData = null, int count = 1)
    {
        if (singleModels.Length < count)
        {
            singleModels = new Matrix[count];
            singleInverseModels = new Matrix[count];
            singleObjectIndices = new uint[count];
            singleDrawData = new Int4[count];
            singleMaterialIndices = new int[count];
        }

        var resolvedDrawData = drawData ?? new Int4(-1, -1, -1, -1);
        var materialArrayIndex = material.MaterialArrayIndex;
        for (int i = 0; i < count; ++i)
        {
            singleModels[i] = localToWorld;
            singleInverseModels[i] = worldToLocal;
            singleObjectIndices[i] = objectIndex;
            singleDrawData[i] = resolvedDrawData;
            singleMaterialIndices[i] = materialArrayIndex;
        }

        var modelsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Matrix>)singleModels.AsSpan(0, count));
        var inverseModelsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Matrix>)singleInverseModels.AsSpan(0, count));
        var objectIndicesBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<uint>)singleObjectIndices.AsSpan(0, count));
        var drawDataBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Int4>)singleDrawData.AsSpan(0, count));
        var materialIndexBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<int>)singleMaterialIndices.AsSpan(0, count));

        SetBuffer("InstancingModels", modelsBuffer);
        SetBuffer("InstancingInverseModels", inverseModelsBuffer);
        SetBuffer("InstancingObjectIndices", objectIndicesBuffer);
        SetBuffer("InstancingDrawData", drawDataBuffer);
        SetBuffer("InstancingMaterialIndex", materialIndexBuffer);
    }

    /// <summary>
    /// Per-instance variant of <see cref="PrepareInstancingData(Material, in Matrix, in Matrix, uint, Int4?, int)"/>:
    /// uploads one transform AND one draw-data int4 per instance, so a single instanced draw can
    /// render N differently-placed objects (each read through gl_InstanceIndex). The inverse-model,
    /// picking-index and material-index SSBOs are filled too, so every Instancing buffer the shader
    /// macros reference stays bound (an unbound one page-faults the GPU). <paramref name="models"/>
    /// and <paramref name="drawData"/> must have the same length.
    /// </summary>
    public void PrepareInstancingData(Material material, ReadOnlySpan<Matrix> models, ReadOnlySpan<Int4> drawData)
    {
        int count = models.Length;
        Debug.Assert(drawData.Length == count, "models and drawData must have the same length");

        if (singleInverseModels.Length < count)
        {
            singleInverseModels = new Matrix[count];
            singleObjectIndices = new uint[count];
            singleMaterialIndices = new int[count];
        }

        var materialArrayIndex = material.MaterialArrayIndex;
        for (int i = 0; i < count; ++i)
        {
            Matrix.Invert(models[i], out singleInverseModels[i]);
            singleObjectIndices[i] = 0;
            singleMaterialIndices[i] = materialArrayIndex;
        }

        var modelsBuffer = commandList.UploadTransientBuffer(models);
        var inverseModelsBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<Matrix>)singleInverseModels.AsSpan(0, count));
        var objectIndicesBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<uint>)singleObjectIndices.AsSpan(0, count));
        var drawDataBuffer = commandList.UploadTransientBuffer(drawData);
        var materialIndexBuffer = commandList.UploadTransientBuffer((ReadOnlySpan<int>)singleMaterialIndices.AsSpan(0, count));

        SetBuffer("InstancingModels", modelsBuffer);
        SetBuffer("InstancingInverseModels", inverseModelsBuffer);
        SetBuffer("InstancingObjectIndices", objectIndicesBuffer);
        SetBuffer("InstancingDrawData", drawDataBuffer);
        SetBuffer("InstancingMaterialIndex", materialIndexBuffer);
    }

    public INativeBuffer UploadTransientBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged
        => commandList.UploadTransientBuffer(data);

    // The mesh travels with the draw rather than through a bind command: the underlying
    // command list caches the binding, so consecutive draws with the same mesh stay as
    // cheap as an explicit bind, and a stage can never draw with a stale mesh.

    /// <summary>Non-indexed draw of the mesh's vertices. Topology comes from the bound pipeline.</summary>
    public void Draw(IMesh mesh, int vertexCount, int firstVertex = 0)
    {
        commandList.BindMesh(mesh);
        stats.NonInstancedDraws++;
        commandList.Draw(vertexCount, firstVertex);
    }

    public void DrawIndexed(IMesh mesh, int submesh)
    {
        var indexCount = mesh.IndexCount(submesh);
        commandList.BindMesh(mesh);
        stats.IndicesDrawn += indexCount;
        stats.TrianglesDrawn += indexCount / 3;
        stats.NonInstancedDraws++;
        commandList.DrawIndexed(indexCount, mesh.IndexStart(submesh), 0);
    }

    public void DrawIndexedInstanced(IMesh mesh, int submesh, int instanceCount, int firstInstance = 0)
    {
        var indexCount = mesh.IndexCount(submesh);
        commandList.BindMesh(mesh);
        stats.IndicesDrawn += indexCount * instanceCount;
        stats.TrianglesDrawn += indexCount / 3 * instanceCount;
        stats.InstancedDraws++;
        stats.InstancedDrawSaved += instanceCount - 1;
        commandList.DrawIndexedInstanced(indexCount, instanceCount, mesh.IndexStart(submesh), 0, firstInstance);
    }

    /// <summary>Issues <paramref name="drawCount"/> indexed draws read from <paramref name="indirectBuffer"/>,
    /// each entry carrying its own index range, instance count and base instance.
    /// <paramref name="totalIndexCount"/>/<paramref name="totalInstanceCount"/> are the sums across all
    /// <paramref name="drawCount"/> entries, used only for stats.</summary>
    public void DrawIndexedIndirect(IMesh mesh, INativeBuffer indirectBuffer, int offset, int drawCount, int totalIndexCount, int totalInstanceCount)
    {
        commandList.BindMesh(mesh);
        stats.IndicesDrawn += totalIndexCount;
        stats.TrianglesDrawn += totalIndexCount / 3;
        stats.InstancedDraws += drawCount;
        stats.InstancedDrawSaved += totalInstanceCount - drawCount;
        commandList.DrawIndexedIndirect(indirectBuffer, offset, drawCount, System.Runtime.InteropServices.Marshal.SizeOf<DrawIndexedIndirectCommand>());
    }

    public void InsertDebugMarker(string label) => commandList.InsertDebugMarker(label);

    /// <summary>Begins a rendering pass into a custom render texture, ending any current pass first.</summary>
    public void BeginRenderTexture(ITexture target, Color4? clearColor = null, LoadOp? depthLoadOp = null)
    {
        if (commandList.InRenderingPass)
            commandList.EndRenderingPass();
        commandList.BeginRenderingPass(new RenderPassDescriptor
        {
            Target = target,
            ColorLoadOp = clearColor.HasValue ? LoadOp.Clear : LoadOp.Load,
            ClearColor = clearColor ?? default,
            DepthLoadOp = depthLoadOp,
            ViewportScale = 1,
        });
    }

    /// <summary>Ends the current rendering pass (pair with <see cref="BeginRenderTexture"/>).</summary>
    public void EndRenderingPass() => commandList.EndRenderingPass();

    /// <summary>Transitions a render texture from render-target to shader-read so a subsequent pass
    /// can sample it. Call after <see cref="EndRenderingPass"/>.</summary>
    public void BarrierToShaderRead(ITexture texture)
        => commandList.Barrier(texture, ResourceUsage.RenderTarget, ResourceUsage.ShaderRead);
}
