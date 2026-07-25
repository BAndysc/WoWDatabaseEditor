using Silk.NET.Vulkan;
using TheEngine;
using TheEngine.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheEngine.Structures;
using VkIndexType = Silk.NET.Vulkan.IndexType;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Rendering;

/// <summary>
/// The single surface through which all draw commands are issued:
///  - all draw state comes from the immutable <see cref="Pipeline"/> bound via
///    <see cref="SetPipeline"/>; there are no loose state-change commands,
///  - the primitive topology comes from the bound pipeline's description,
///    not from the draw call,
///  - the index format is bound together with the mesh (= vertex/index buffers),
///    not passed per draw.
///
/// The implementation (<see cref="Vulkan.VulkanCommandList"/>) records every command
/// into the backend's current frame command buffer, submitted in EndFrame.
/// </summary>
public interface ICommandList : IDisposable
{
    /// <summary>Resets all cached binding state. Call once at the start of a frame.</summary>
    void Begin();

    /// <summary>
    /// Ends the frame's command recording (vkEndCommandBuffer); no commands may follow
    /// until the next <see cref="Begin"/>. The backend submits the buffer in EndFrame.
    /// </summary>
    void End();

    /// <summary>
    /// Starts a rendering pass into the given target (vkCmdBeginRendering). All draws must
    /// happen inside a pass; the descriptor's load ops control clearing.
    /// </summary>
    void BeginRenderingPass(in RenderPassDescriptor descriptor);

    /// <summary>Ends the current rendering pass. Blits and barriers are only legal outside a pass.</summary>
    void EndRenderingPass();

    /// <summary>True between <see cref="BeginRenderingPass"/> and <see cref="EndRenderingPass"/>.</summary>
    bool InRenderingPass { get; }

    /// <summary>
    /// Copies (with optional scaling) a region of one render texture to another. Must be called
    /// outside a rendering pass (maps to vkCmdBlitImage, which operates on transfer-usage images).
    /// </summary>
    void Blit(ITexture source, ITexture destination, int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, BlitMask mask, BlitFilter filter);

    /// <summary>
    /// Declares a usage transition of a texture (e.g. rendered-to, then sampled): an image
    /// memory barrier with a layout transition. Must be called outside a rendering pass.
    /// </summary>
    void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to);

    /// <summary>Sets the dynamic depth bias for subsequent draws in the current pass (constant +
    /// slope-scaled). Reset to 0 at the start of every pass; used by the cascaded shadow pass to
    /// suppress shadow acne.</summary>
    void SetDepthBias(float constantFactor, float slopeFactor);

    /// <summary>Overrides the depth compare op / write enable for subsequent draws regardless of the
    /// bound pipeline's baked state (null restores the baked state). Lets the engine run the opaque
    /// pass as Equal + write-off after the depth prepass without changing any material.</summary>
    void SetDepthStateOverride(Veldrid.ComparisonKind? comparison, bool? writeEnabled);

    /// <summary>
    /// Binds the pipeline state and shader program. The (pipeline, shaderPass) pair
    /// is the identity of the backend pipeline object: the same Pipeline can be
    /// drawn with different passes (forward/depth/...), each combination compiling to
    /// a separate immutable Vulkan pipeline (resolved lazily at draw time against the
    /// current pass's attachment formats).
    /// </summary>
    void SetPipeline(Pipeline pipeline, IShaderPass shaderPass);

    /// <summary>
    /// Sets the scissor rectangle for subsequent draws (vkCmdSetScissor - dynamic state).
    /// Only takes effect for pipelines whose rasterizer state enables scissor testing.
    /// Coordinates are origin top-left, in pixels of the current rendering pass target.
    /// </summary>
    void SetScissor(int x, int y, int width, int height);

    /// <summary>
    /// Binds the material's resources - textures, structured buffers and the material's
    /// constant data - for the currently bound pipeline: the per-material descriptor set
    /// (vkCmdBindDescriptorSets, set 1) and its small POD data as push constants
    /// (vkCmdPushConstants). Requires a pipeline bound via
    /// <see cref="SetPipeline"/> with this material's pipeline. Buffers queued via
    /// <see cref="SetBuffer"/> since the last bind override the material's own buffer of the
    /// same name, then the pending queue is cleared.
    /// </summary>
    void BindMaterialResources(Material material);

    /// <summary>
    /// Queues a structured-buffer binding that overrides/extends the material's own buffers for
    /// the next <see cref="BindMaterialResources(Material)"/> call (e.g. per-draw instancing SSBOs,
    /// glyph/line-vertex buffers). Consumed and cleared by that call.
    /// </summary>
    void SetBuffer(GlobalUniformHandle uniform, INativeBuffer buffer);

    /// <summary>
    /// Like <see cref="SetBuffer"/>, but the binding persists across <see cref="BindMaterialResources(Material)"/>
    /// calls instead of being consumed per draw - for buffers that stay constant over many draws
    /// (e.g. the per-frame instancing SSBOs shared by every batch in a render range). Set once,
    /// then cleared with <see cref="ClearPersistentBuffers"/>; per-draw <see cref="SetBuffer"/>
    /// bindings of the same uniform still take precedence for that draw.
    /// </summary>
    void SetPersistentBuffer(GlobalUniformHandle uniform, INativeBuffer buffer);

    /// <summary>Clears all bindings set via <see cref="SetPersistentBuffer"/>.</summary>
    void ClearPersistentBuffers();

    /// <summary>
    /// Sets the per-draw bindless texture-index push constant (the packed index from
    /// <see cref="ITextureManager.GetBindlessIndex"/>) for the currently bound pipeline. Lets a
    /// shader pick its texture per draw without rewriting set 1; used by ImGui.
    /// </summary>
    void SetBindlessTextureIndex(int packedIndex);

    /// <summary>
    /// Binds a uniform buffer to a global binding slot shared by all pipelines
    /// (SceneData/ObjectData), through the per-frame descriptor set (set 0).
    /// </summary>
    void BindUniformBuffer(int slot, INativeBuffer buffer);

    /// <summary>
    /// Copies the data into a transient slice of GPU memory owned by the command list and
    /// binds it to a global uniform-buffer slot (SceneData/ObjectData). Draws recorded after
    /// this call read this snapshot; the slot can be re-bound with new data between draws.
    /// The slice is valid only for the current frame: a suballocation from a per-frame
    /// ring buffer bound through a dynamic uniform-buffer offset in set 0.
    /// </summary>
    void BindTransientUniformBuffer<T>(int slot, ref T data) where T : unmanaged;

    /// <summary>
    /// Raw-bytes form of <see cref="BindTransientUniformBuffer{T}"/>, used when the data
    /// was snapshotted earlier (deferred replay reads it back out of the command stream).
    /// </summary>
    void BindTransientUniformBuffer(int slot, ReadOnlySpan<byte> data);

    /// <summary>
    /// Copies the data into a transient structured buffer owned by the command list and returns
    /// a handle that can be bound for draws recorded later this frame (e.g. through
    /// <see cref="SetBuffer"/>). Every call returns an independent
    /// slice, so the caller never overwrites data still referenced by earlier draws.
    /// The slice is valid only for the current frame: a suballocation from a per-frame
    /// ring buffer bound as a std430 SSBO.
    /// </summary>
    INativeBuffer UploadTransientBuffer<T>(ReadOnlySpan<T> data) where T : unmanaged;

    /// <summary>
    /// Copies the data into a transient vertex or index buffer owned by the command list
    /// (per-frame streamed geometry like ImGui). Same lifetime rules as the structured
    /// overload: the returned handle is valid only for the current frame and is bound
    /// through <see cref="BindVertexBuffer"/>/<see cref="BindIndexBuffer"/>.
    /// </summary>
    INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged;

    /// <summary>
    /// Copies the current frame's point lights into the per-frame Light SSBO (set 0,
    /// binding <see cref="Constants.LIGHT_BUFFER_BINDING"/>), read by the
    /// light-culling compute pass and the forward shading lighting() loop. Writes directly
    /// into host-visible/coherent mapped memory, so unlike most uploads this is not
    /// order-dependent and is applied immediately rather than recorded.
    /// </summary>
    void UploadLightData(ReadOnlySpan<GpuPointLight> lights);

    /// <summary>
    /// Copies the current frame's decals into the per-frame Decal SSBO (set 0, binding
    /// <see cref="Constants.DECAL_BUFFER_BINDING"/>), read by the merged
    /// tile-culling compute pass and the forward shading ApplyDecals() call. Same immediate,
    /// not-order-dependent semantics as <see cref="UploadLightData"/>.
    /// </summary>
    void UploadDecalData(ReadOnlySpan<GpuDecal> decals);

    /// <summary>
    /// Dispatches the merged Forward+ tile light/decal-culling compute pass
    /// (internalShaders/tile_cull.comp) over a <paramref name="tilesX"/> x <paramref name="tilesY"/>
    /// workgroup grid, reading the depth prepass texture (via its bindless slot,
    /// <paramref name="depthTextureIndex"/>) and the Light/Decal SSBOs, and writing
    /// LightGrid/LightIndexList and DecalGrid/DecalIndexList (set 0, bindings
    /// <see cref="Constants.LIGHT_GRID_BINDING"/>/<see cref="Constants.LIGHT_INDEX_LIST_BINDING"/>/
    /// <see cref="Constants.DECAL_GRID_BINDING"/>/<see cref="Constants.DECAL_INDEX_LIST_BINDING"/>).
    /// Must be called outside a rendering pass; includes the trailing barrier that makes the
    /// grid/index buffers visible to the forward fragment shaders.
    /// Pass <paramref name="isSceneView"/> true to cull against - and write into - the editor
    /// scene view's own independent grid/index set (bindings
    /// <see cref="Constants.SCENE_LIGHT_GRID_BINDING"/> etc.) instead of the
    /// main view's, so the two views' results can coexist within the same frame.
    /// </summary>
    void DispatchTileCullCompute(int depthTextureIndex, int screenWidth, int screenHeight, int tilesX, int tilesY, bool isSceneView = false);

    /// <summary>Binds the mesh's vertex and index buffers. Null unbinds nothing but invalidates the cache (for externally managed state like ImGui).</summary>
    void BindMesh(IMesh? mesh);

    /// <summary>
    /// Binds a vertex buffer directly (vkCmdBindVertexBuffers), bypassing the mesh abstraction;
    /// invalidates the mesh binding. The attribute layout comes from the bound pipeline.
    /// </summary>
    void BindVertexBuffer(INativeBuffer buffer);

    /// <summary>
    /// Binds an index buffer and its index format directly (vkCmdBindIndexBuffer),
    /// bypassing the mesh abstraction; invalidates the mesh binding.
    /// </summary>
    void BindIndexBuffer(INativeBuffer buffer, VkIndexType indexType);

    /// <summary>Non-indexed draw. Topology comes from the bound pipeline.</summary>
    void Draw(int vertexCount, int firstVertex);

    /// <summary>Indexed draw. Index format comes from the bound mesh, topology from the bound pipeline.</summary>
    void DrawIndexed(int indexCount, int firstIndex, int baseVertex);

    /// <summary>Indexed instanced draw. Index format comes from the bound mesh, topology from the bound pipeline.</summary>
    void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance);

    /// <summary>
    /// Issues <paramref name="drawCount"/> indexed draws read from <paramref name="indirectBuffer"/>
    /// (an array of <see cref="DrawIndexedIndirectCommand"/>, starting at byte <paramref name="offset"/>,
    /// <paramref name="stride"/> bytes apart). Index format comes from the bound mesh, topology from
    /// the bound pipeline.
    /// </summary>
    void DrawIndexedIndirect(INativeBuffer indirectBuffer, int offset, int drawCount, int stride);

    /// <summary>
    /// Synchronously reads back pixels from a render texture's R32ui color attachment
    /// (object picking). Must be called outside a rendering pass. Copies into a staging
    /// buffer and waits a fence - a full GPU sync, so never call it per frame; use
    /// <see cref="ReadPixelDeferred"/> for continuous reads.
    /// </summary>
    void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination);

    /// <summary>
    /// Deferred (stall-free) 1×1 pixel readback for continuous hover picking: records a copy of the
    /// pixel into a per-(channel, frame-in-flight) staging slot inside the current frame's command
    /// stream and returns the value that landed in the same slot frames-in-flight frames ago (its
    /// fence was already waited at BeginFrame, so no GPU sync happens). Call every frame; the result
    /// lags a couple of frames (i.e. it is the pixel the user actually sees). Null until the first
    /// request completes. Must be called outside a rendering pass; recorded at the point of call, so
    /// when called during the game update it reads the PREVIOUS frame's image. Independent per-frame
    /// readers must use distinct channels (a second same-channel call in one frame clobbers the slot).
    /// For a depth attachment pass the depth ITexture itself; the value is the raw depth float's bits.
    /// </summary>
    uint? ReadPixelDeferred(ITexture texture, int colorAttachment, int x, int y, int channel = 0);

    /// <summary>The frame-in-flight index the next <see cref="ReadPixelDeferred"/> writes to - lets
    /// callers ring-buffer their own per-request metadata (e.g. the camera matrices that rendered
    /// the frame being read) in lockstep with the returned values.</summary>
    int DeferredReadSlot { get; }

    /// <summary>Inserts a debug label into the command stream (vkCmdInsertDebugUtilsLabel). Debug aid only.</summary>
    void InsertDebugMarker(string label);

    /// <summary>Debug aid: reports (to the console) validation-layer errors that occurred since the
    /// last check, tagged with <paramref name="context"/> to localize them within the frame. Cheap;
    /// does nothing when the validation layers are inactive.</summary>
    void CheckError(string context);

    /// <summary>Number of shader program switches since <see cref="Begin"/>.</summary>
    int ShaderSwitches { get; }

    /// <summary>Number of mesh (vertex/index buffer) switches since <see cref="Begin"/>.</summary>
    int MeshSwitches { get; }

    /// <summary>Number of set=1 descriptor set rebinds that reused a cached descriptor set since <see cref="Begin"/>.</summary>
    int Set1CacheHits { get; }

    /// <summary>Number of set=1 descriptor sets allocated and written from scratch since <see cref="Begin"/>.</summary>
    int Set1CacheMisses { get; }
}

/// <summary>Layout matches VkDrawIndexedIndirectCommand, for use with <see cref="ICommandList.DrawIndexedIndirect"/>.</summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct DrawIndexedIndirectCommand
{
    public uint IndexCount;
    public uint InstanceCount;
    public uint FirstIndex;
    public int VertexOffset;
    public uint FirstInstance;
}
