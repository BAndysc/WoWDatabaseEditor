using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
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
    private ObjectBuffer objectData;

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

    /// <summary>Binds the pipeline state, shader pass and the material's resources.</summary>
    public void SetMaterial(Material material, IShaderPass shaderPass, MaterialInstanceRenderData? instanceData = null)
    {
        commandList.SetPipeline(material.Pipeline, shaderPass);
        commandList.BindMaterialResources(material, instanceData);
        stats.MaterialActivations++;
    }

    public void SetMaterial(Material material, ShaderPassType shaderPassType, MaterialInstanceRenderData? instanceData = null)
        => SetMaterial(material, material.GetShaderPass(shaderPassType, false)!, instanceData);

    /// <summary>
    /// Uploads the per-draw object data (world matrix, picking index, per-draw ints)
    /// as a transient snapshot for draws recorded after this call.
    /// </summary>
    public void SetObjectData(in Matrix localToWorld, in Matrix worldToLocal, uint objectIndex = 0, Int4? drawData = null)
    {
        objectData.WorldMatrix = localToWorld;
        objectData.InverseWorldMatrix = worldToLocal;
        objectData.ObjectIndex = objectIndex;
        objectData.DrawData = drawData ?? new Int4(-1, -1, -1, -1);
        commandList.BindTransientUniformBuffer(Constants.OBJECT_BUFFER_INDEX, ref objectData);
    }

    // re-binds whatever object data was staged last, so the slot is never unbound
    internal void RebindObjectData()
        => commandList.BindTransientUniformBuffer(Constants.OBJECT_BUFFER_INDEX, ref objectData);

    public INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged
        => commandList.UploadTransientBuffer(format, data);

    // The mesh travels with the draw rather than through a bind command: the underlying
    // command list caches the binding, so consecutive draws with the same mesh stay as
    // cheap as an explicit bind, and a stage can never draw with a stale mesh.

    /// <summary>Non-indexed draw of the mesh's vertices. Topology comes from the bound pipeline.</summary>
    public void Draw(IMesh mesh, int vertexCount, int firstVertex = 0)
    {
        commandList.BindMesh(mesh);
        commandList.ValidateState();
        stats.NonInstancedDraws++;
        commandList.Draw(vertexCount, firstVertex);
    }

    public void DrawIndexed(IMesh mesh, int submesh)
    {
        var indexCount = mesh.IndexCount(submesh);
        commandList.BindMesh(mesh);
        commandList.ValidateState();
        stats.IndicesDrawn += indexCount;
        stats.TrianglesDrawn += indexCount / 3;
        stats.NonInstancedDraws++;
        commandList.DrawIndexed(indexCount, mesh.IndexStart(submesh), 0);
    }

    public void DrawIndexedInstanced(IMesh mesh, int submesh, int instanceCount)
    {
        var indexCount = mesh.IndexCount(submesh);
        commandList.BindMesh(mesh);
        commandList.ValidateState();
        stats.IndicesDrawn += indexCount * instanceCount;
        stats.TrianglesDrawn += indexCount / 3 * instanceCount;
        stats.InstancedDraws++;
        stats.InstancedDrawSaved += instanceCount - 1;
        commandList.DrawIndexedInstanced(indexCount, instanceCount, mesh.IndexStart(submesh), 0, 0);
    }

    public void InsertDebugMarker(string label) => commandList.InsertDebugMarker(label);
}
