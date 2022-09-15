using SixLabors.ImageSharp.PixelFormats;
using TheAvaloniaOpenGL.Resources;
using TheEngine.Handles;
using TheMaths;

namespace TheEngine.Interfaces
{
    public interface ITextureManager
    {
        TextureHandle EmptyTexture { get; }
        TextureHandle LoadTexture(string path);
        //TextureHandle LoadTextureArray(params string[] path);
        TextureHandle CreateTextureArray(Rgba32[][][] pixels, int width, int height);
        TextureHandle CreateTexture(Rgba32[][] pixels, int width, int height, bool generateMips);
        TextureHandle CreateTexture(uint[] pixels, int width, int height, TextureFormat format = TextureFormat.R8G8B8A8);
        TextureHandle CreateTexture(float[] pixels, int width, int height); 
        TextureHandle CreateTexture(Vector4[] pixels, int width, int height);
        TextureHandle CreateRenderTexture(int width, int height, int colorAttachments = 1);
        void ScreenshotRenderTexture(TextureHandle rt, string fileName, int colorAttachmentIndex = 0);
        void BlitRenderTextures(TextureHandle src, TextureHandle dst);
        void DisposeTexture(TextureHandle handle);
        void SetFiltering(TextureHandle handle, FilteringMode mode);
        void SetWrapping(TextureHandle handle, WrapMode mode);
        TextureHandle CreateDummyHandle();
        /**
         * old handle gets @new texture contents
         * @new handle becomes invalid
         */
        void ReplaceHandles(TextureHandle old, TextureHandle @new);

    }
}
