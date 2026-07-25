using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Entities;
using TheEngine.Managers;
using TheMaths;

namespace TheEngine.Rendering;

/// <summary>
/// The complete surface a rendering backend provides to the engine: resource creation,
/// shader loading, the command-list executor and the per-frame lifecycle. Everything else
/// in the engine (managers, render stages, materials) is backend-neutral and records
/// through <see cref="ICommandList"/>.
///
/// Conventions shared by all backends (the GL implementation is the reference):
///  - render texture contents are stored bottom-up (GL memory order); uv (0,0) samples
///    the bottom-left of the rendered image. A non-GL backend must rasterize so that its
///    images match this layout (GL clip space + positive viewport does this on Vulkan),
///    and flip once when writing the final image into the window surface,
///  - scissor rectangles are recorded top-left (window convention) and flipped by the
///    executor against the current pass height,
///  - <see cref="ScreenshotRenderTexture"/> writes a top-down (correctly oriented) PNG.
/// </summary>
internal interface IRenderBackend : IDisposable
{
    string Name { get; }

    /// <summary>Creates the executor command list that deferred recording replays into.</summary>
    ICommandList CreateExecutor(TextureManager textureManager);

    /// <summary>Called at the very start of a frame, before any recording (GL: state reset; Vulkan: acquire image, reset per-frame pools).</summary>
    void BeginFrame();

    /// <summary>Called after FinalizeRendering executed the frame (Vulkan: submit + present; GL: no-op, the window swaps).</summary>
    void EndFrame();

    INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, int size, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged;
    INativeBuffer<T> CreateBuffer<T>(BufferTypeEnum bufferType, ReadOnlySpan<T> data, BufferInternalFormat format = BufferInternalFormat.None) where T : unmanaged;

    INativeTexture CreateTexture(int width, int height, Rgba32[][] pixels, bool generateMips);
    unsafe INativeTexture CreateTexture(int width, int height, Rgba32* pixels, bool generateMips);
    INativeTexture CreateTexture(int width, int height, Vector4[] pixels);
    INativeTexture CreateTexture(int width, int height, float[] pixels);
    INativeTexture CreateTexture(int width, int height, uint[]? pixels, TextureFormat format);
    INativeTexture CreateTextureArray(int width, int height, Rgba32[][][] pixels);

    INativeTexture CreateRenderTexture(int width, int height, int colorAttachments = 1, INativeTexture? depthTexture = null);
    INativeTexture CreateRenderTexture(INativeTexture colorAttachment, INativeTexture depthTexture, INativeTexture? colorAttachment1 = null);

    IShader LoadShader(string jsonPath, string[] includePaths);

    /// <summary>Called after a mesh's vertex/index buffers exist (GL: capture the vertex layout in a VAO).</summary>
    void OnMeshCreated(Mesh mesh);

    /// <summary>Synchronously reads back a render texture's color attachment and saves a correctly oriented PNG.</summary>
    void ScreenshotRenderTexture(INativeTexture renderTexture, string fileName, int colorAttachmentIndex);

    /// <summary>Frees GPU resources whose owners were garbage collected (called once per frame, on the render thread).</summary>
    void CollectDisposedResources();

    long TotalBufferBytes { get; }
}
