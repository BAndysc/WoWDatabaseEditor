using SixLabors.ImageSharp.PixelFormats;
using TheEngine.Resources;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface ITextureManager
    {
        ITexture EmptyTexture { get; }
        ITexture LoadTexture(string path);
        //TextureHandle LoadTextureArray(params string[] path);
        ITexture CreateTextureArray(Rgba32[][][] pixels, int width, int height);
        ITexture CreateTexture(Rgba32[][] pixels, int width, int height, bool generateMips);

        /// <summary>Asynchronously creates a sampled texture: the GPU upload runs on the transfer queue
        /// off the render thread. The returned ValueTask completes (on the render thread) once the texture
        /// is ready to sample. Safe to call from a worker thread.</summary>
        System.Threading.Tasks.ValueTask<ITexture> CreateTextureAsync(Rgba32[][] pixels, int width, int height, bool generateMips, FilteringMode filtering, WrapMode wrapping);
        ITexture CreateTexture(uint[]? pixels, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8);
        ITexture CreateTexture(float[] pixels, int width, int height);
        ITexture CreateTexture(Vector4[] pixels, int width, int height);
        ITexture CreateRenderTexture(int width, int height, int colorAttachments = 1);
        /// <summary>Combines explicit color/depth/second-color attachments into a render texture (e.g. to
        /// match the main object buffer's attachment layout for off-screen object rendering).</summary>
        ITexture CreateRenderTexture(ITexture colorTexture, ITexture depthTexture, ITexture colorTexture1);
        ITexture CreateDepthOnlyRenderTexture(ITexture depthTexture);
        void ScreenshotRenderTexture(ITexture rt, string fileName, int colorAttachmentIndex = 0);
        void DisposeTexture(ITexture? tex);
        void SetFiltering(ITexture texture, FilteringMode mode);
        void SetWrapping(ITexture texture, WrapMode mode);

        /// <summary>Registers (or returns the existing) bindless texture-array slot, for use as a texture index in material data.</summary>
        int GetBindlessIndex(ITexture texture);
    }

    public interface ITexture
    {
        TextureHandle Handle { get; }
        int Width { get; }
        int Height { get; }
    }
}
