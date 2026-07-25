using TheAvaloniaOpenGL;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Components;
using TheEngine.Entities;
using TheEngine.Interfaces;
using Pipeline = TheEngine.Resources.Pipeline;

namespace TheEngine.Rendering;

/// <summary>
/// The single surface through which all draw commands are issued.
///
/// The vocabulary is deliberately Vulkan-shaped so that a future backend can
/// record these calls into a command buffer:
///  - all draw state comes from the immutable <see cref="Pipeline"/> bound via
///    <see cref="SetPipeline"/>; there are no loose state-change commands,
///  - the primitive topology comes from the bound pipeline's description,
///    not from the draw call,
///  - the index format is bound together with the mesh (= vertex/index buffers),
///    not passed per draw.
///
/// The current implementation (<see cref="ImmediateCommandList"/>) executes
/// every command immediately on the GL context of the calling thread.
/// </summary>
public interface ICommandList : IDisposable
{
    /// <summary>Resets all cached binding state. Call once at the start of a frame.</summary>
    void Begin();

    /// <summary>
    /// Ends the frame's command recording; no commands may follow until the next <see cref="Begin"/>.
    /// On Vulkan this becomes vkEndCommandBuffer + queue submission; on GL a no-op,
    /// because every command has already executed.
    /// </summary>
    void End();

    /// <summary>
    /// Starts a rendering pass into the given target. All draws must happen inside a pass.
    /// On GL this binds the framebuffer, sets the viewport and optionally clears;
    /// on Vulkan it maps to vkCmdBeginRendering.
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
    /// Declares a usage transition of a texture (e.g. rendered-to, then sampled). A no-op on GL;
    /// on Vulkan this becomes an image memory barrier with a layout transition. Must be called
    /// outside a rendering pass.
    /// </summary>
    void Barrier(ITexture texture, ResourceUsage from, ResourceUsage to);

    /// <summary>
    /// Binds the pipeline state and shader program. The (pipeline, shaderPass) pair
    /// is the identity of a future backend pipeline object: the same Pipeline can be
    /// drawn with different passes (forward/instanced/...), each combination would
    /// compile to a separate immutable pipeline on Vulkan.
    /// </summary>
    void SetPipeline(Pipeline pipeline, IShaderPass shaderPass);

    /// <summary>
    /// Sets the scissor rectangle for subsequent draws (vkCmdSetScissor - dynamic state).
    /// Only takes effect for pipelines whose rasterizer state enables scissor testing.
    /// Coordinates are in Vulkan convention: origin top-left, in pixels of the current
    /// rendering pass target. The GL executor flips to bottom-left internally.
    /// </summary>
    void SetScissor(int x, int y, int width, int height);

    /// <summary>
    /// Binds the material's resources - textures, structured buffers and the material's
    /// constant data - for the currently bound pipeline. The material's resources are the
    /// future per-material descriptor set (vkCmdBindDescriptorSets) and its small POD data
    /// the future push constants (vkCmdPushConstants). Requires a pipeline bound via
    /// <see cref="SetPipeline"/> with this material's pipeline.
    /// </summary>
    void BindMaterialResources(Material material, MaterialInstanceRenderData? instanceData = null);

    /// <summary>
    /// Binds material resources from a record-time snapshot instead of the material's live
    /// (mutable) state. Used by deferred recording: the snapshot is captured when the bind is
    /// recorded, so materials mutated between draws still replay with the right state.
    /// </summary>
    void BindMaterialResources(Material material, MaterialSnapshot snapshot);

    /// <summary>
    /// Binds a uniform buffer to a global binding slot shared by all pipelines
    /// (SceneData/ObjectData). The equivalent of the per-frame descriptor set (set 0)
    /// on Vulkan; glBindBufferBase on GL.
    /// </summary>
    void BindUniformBuffer(int slot, INativeBuffer buffer);

    /// <summary>
    /// Copies the data into a transient slice of GPU memory owned by the command list and
    /// binds it to a global uniform-buffer slot (SceneData/ObjectData). Draws recorded after
    /// this call read this snapshot; the slot can be re-bound with new data between draws.
    /// The slice is valid only for the current frame.
    /// On Vulkan this becomes a suballocation from a per-frame ring buffer bound through a
    /// dynamic uniform-buffer offset in set 0; on GL an orphaned uniform buffer + glBindBufferBase.
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
    /// <see cref="MaterialInstanceRenderData.SetBuffer"/>). Every call returns an independent
    /// slice, so the caller never overwrites data still referenced by earlier draws.
    /// The slice is valid only for the current frame.
    /// On Vulkan this becomes a suballocation from a per-frame ring buffer exposed through a
    /// transient buffer view; on GL an orphaned buffer object from a per-frame pool.
    /// </summary>
    INativeBuffer UploadTransientBuffer<T>(BufferInternalFormat format, ReadOnlySpan<T> data) where T : unmanaged;

    /// <summary>
    /// Copies the data into a transient vertex or index buffer owned by the command list
    /// (per-frame streamed geometry like ImGui). Same lifetime rules as the structured
    /// overload: the returned handle is valid only for the current frame and is bound
    /// through <see cref="BindVertexBuffer"/>/<see cref="BindIndexBuffer"/>.
    /// </summary>
    INativeBuffer UploadTransientBuffer<T>(BufferTypeEnum type, ReadOnlySpan<T> data) where T : unmanaged;

    /// <summary>Binds the mesh's vertex and index buffers (a VAO on GL). Null unbinds nothing but invalidates the cache (transitional, for externally managed state like ImGui).</summary>
    void BindMesh(IMesh? mesh);

    /// <summary>
    /// Binds a vertex buffer directly (vkCmdBindVertexBuffers), bypassing the mesh abstraction;
    /// invalidates the mesh binding. The attribute layout is the caller's responsibility:
    /// on Vulkan it is part of the pipeline, on GL 4.1 it must be captured in a VAO
    /// against this concrete buffer object after this call.
    /// </summary>
    void BindVertexBuffer(INativeBuffer buffer);

    /// <summary>
    /// Binds an index buffer and its index format directly (vkCmdBindIndexBuffer),
    /// bypassing the mesh abstraction; invalidates the mesh binding.
    /// </summary>
    void BindIndexBuffer(INativeBuffer buffer, IndexType indexType);

    /// <summary>Non-indexed draw. Topology comes from the bound pipeline.</summary>
    void Draw(int vertexCount, int firstVertex);

    /// <summary>Indexed draw. Index format comes from the bound mesh, topology from the bound pipeline.</summary>
    void DrawIndexed(int indexCount, int firstIndex, int baseVertex);

    /// <summary>Indexed instanced draw. Index format comes from the bound mesh, topology from the bound pipeline.</summary>
    void DrawIndexedInstanced(int indexCount, int instanceCount, int firstIndex, int baseVertex, int firstInstance);

    /// <summary>
    /// Synchronously reads back pixels from a render texture's R32ui color attachment
    /// (object picking). Must be called outside a rendering pass. On GL this is glReadPixels;
    /// on Vulkan it becomes a copy into a staging buffer plus a fence wait
    /// (later: an N-frame-latency readback to avoid the stall).
    /// </summary>
    void ReadPixels(ITexture texture, int colorAttachment, int x, int y, int width, int height, Span<uint> destination);

    /// <summary>Inserts a debug label into the command stream (vkCmdInsertDebugUtilsLabel on Vulkan). Debug aid only.</summary>
    void InsertDebugMarker(string label);

    /// <summary>Transitional debug aid: checks for GL errors. On Vulkan the validation layers take this role and this becomes a no-op.</summary>
    void CheckError(string context);

    /// <summary>Debug-only validation of the currently bound shader against GL state. No-op in release builds.</summary>
    void ValidateState();

    /// <summary>Number of shader program switches since <see cref="Begin"/>.</summary>
    int ShaderSwitches { get; }

    /// <summary>Number of mesh (vertex/index buffer) switches since <see cref="Begin"/>.</summary>
    int MeshSwitches { get; }
}
