using SixLabors.ImageSharp.PixelFormats;
using TheEngine.Resources;
using TheEngine.Entities;
using TheEngine.Managers;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// The complete surface the rendering backend (<see cref="Vulkan.VulkanRenderBackend"/>)
/// provides to the engine: resource creation, shader loading, the command-list executor
/// and the per-frame lifecycle. Everything else in the engine (managers, render stages,
/// materials) records through <see cref="ICommandList"/>.
///
/// Conventions:
///  - native Vulkan orientation: the projection emits ndc z in [0,1] and a negative-height
///    viewport stores render targets top-down, so uv (0,0) samples the top-left of the
///    rendered image and no Y-flip happens at present time,
///  - scissor rectangles are recorded top-left, in pixels of the current pass target,
///  - <see cref="ScreenshotRenderTexture"/> writes a top-down (correctly oriented) PNG.
/// </summary>
internal interface IRenderBackend : IDisposable
{
    string Name { get; }

    /// <summary>Last measured GPU frame time in ms (only populated when THEENGINE_PROFILE=1; 0 otherwise).</summary>
    float LastGpuMs => 0;
    /// <summary>CPU ms blocked in BeginFrame's WaitForFences (GPU sync). Profile-only.</summary>
    float LastFenceWaitMs => 0;
    /// <summary>CPU ms blocked in BeginFrame's AcquireNextImage (present/vsync). Profile-only.</summary>
    float LastAcquireMs => 0;
    /// <summary>CPU ms reading back the GPU timestamp query (profiling artifact). Profile-only.</summary>
    float LastQueryReadbackMs => 0;
    /// <summary>CPU ms in ProcessPendingDestroys. Profile-only.</summary>
    float LastDestroyMs => 0;
    /// <summary>Deferred destroys executed this frame / still queued. Profile-only.</summary>
    int LastDestroyedCount => 0;
    int PendingDestroyQueueLength => 0;

    /// <summary>Whether vsync can be toggled at all. False when presentation is paced by an
    /// external compositor (the Avalonia composition panel) - there vsync is effectively always
    /// on and <see cref="VSync"/> ignores writes.</summary>
    bool SupportsVSyncControl { get; }

    /// <summary>Desired vsync state; applied at the start of the next frame.</summary>
    bool VSync { get; set; }

    /// <summary>Creates the executor command list that deferred recording replays into.</summary>
    ICommandList CreateExecutor(TextureManager textureManager);

    /// <summary>Called at the very start of a frame, before any recording: waits the frame fence, acquires the swapchain image and resets the per-frame pools/ring.</summary>
    void BeginFrame();

    bool InFrame { get; }

    /// <summary>Called after FinalizeRendering recorded the frame: submits the command buffer and presents.</summary>
    void EndFrame();

    INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size) where T : unmanaged;
    INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data) where T : unmanaged;

    INativeTexture CreateTexture(int width, int height, Rgba32[][] pixels, bool generateMips);
    unsafe INativeTexture CreateTexture(int width, int height, Rgba32* pixels, bool generateMips);
    INativeTexture CreateTexture(int width, int height, Vector4[] pixels);
    INativeTexture CreateTexture(int width, int height, float[] pixels);
    INativeTexture CreateTexture(int width, int height, uint[]? pixels, TextureFormat format);
    INativeTexture CreateTextureArray(int width, int height, Rgba32[][][] pixels);

    /// <summary>Asynchronously creates a sampled RGBA8 texture from a pre-built mip chain, staged and
    /// copied on the transfer queue off the render thread. The returned ValueTask completes on the render
    /// thread once the GPU copy is done. Vulkan only.</summary>
    System.Threading.Tasks.ValueTask<INativeTexture> CreateTextureAsync(int width, int height, Rgba32[][] mips, bool generateMips, FilteringMode filtering, WrapMode wrapping);

    INativeTexture CreateRenderTexture(int width, int height, int colorAttachments = 1, INativeTexture? depthTexture = null);
    INativeTexture CreateRenderTexture(INativeTexture colorAttachment, INativeTexture depthTexture, INativeTexture? colorAttachment1 = null);

    IShader LoadShader(string jsonPath, string[] includePaths);

    /// <summary>Synchronously reads back a render texture's color attachment and saves a correctly oriented PNG.</summary>
    void ScreenshotRenderTexture(INativeTexture renderTexture, string fileName, int colorAttachmentIndex);

    /// <summary>Frees GPU resources whose owners were garbage collected (called once per frame, on the render thread).</summary>
    void CollectDisposedResources();

    /// <summary>Registers (or returns the existing) bindless texture-array slot for a texture (Vulkan only).</summary>
    int GetBindlessTextureSlot(INativeTexture texture);

    long TotalBufferBytes { get; }
}
