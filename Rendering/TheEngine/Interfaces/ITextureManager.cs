using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
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
        ITexture CreateTexture(uint[] pixels, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8);
        ITexture CreateTexture(float[] pixels, int width, int height);
        ITexture CreateTexture(Vector4[] pixels, int width, int height);
        ITexture CreateRenderTexture(int width, int height, int colorAttachments = 1);
        void ScreenshotRenderTexture(ITexture rt, string fileName, int colorAttachmentIndex = 0);
        void BlitRenderTextures(ITexture src, ITexture dst);
        void DisposeTexture(ITexture? tex);
        void SetFiltering(ITexture texture, FilteringMode mode);
        void SetWrapping(ITexture texture, WrapMode mode);
    }

    public interface ITexture
    {
        TextureHandle Handle { get; }
        int Width { get; }
        int Height { get; }
    }
}
