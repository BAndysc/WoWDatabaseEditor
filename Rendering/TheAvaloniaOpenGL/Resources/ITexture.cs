namespace TheAvaloniaOpenGL.Resources
{
    public enum TextureFormat
    {
        R8G8B8A8,
        R32f,
        R32ui,
        DepthComponent
    }
    
    public enum FilteringMode
    {
        Linear,
        Nearest
    }
    
    public enum WrapMode
    {
        ClampToEdge,
        ClampToBorder,
        Repeat,
        MirroredRepeat
    }

    public interface ITexture : IDisposable
    {
        int Width { get; }
        int Height { get; }

        void Activate(int slot);
        void SetFiltering(FilteringMode mode);
        void SetWrapping(WrapMode mode);
    }
}
