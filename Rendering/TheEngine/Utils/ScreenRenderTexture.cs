using TheEngine.Handles;

namespace TheEngine.Utils;

public class ScreenRenderTexture : System.IDisposable
{
    private readonly Engine engine;
    private readonly float scale;
    private int width = -1;
    private int height = -1;
    
    public ScreenRenderTexture(Engine engine, float scale = 1)
    {
        this.engine = engine;
        this.scale = scale;
        Update();
    }
    
    public TextureHandle Handle { get; private set; }
    
    public static implicit operator TextureHandle(ScreenRenderTexture d) => d.Handle;

    public void Update()
    {
        if (width != (int)engine.WindowHost.WindowWidth ||
            height != (int)engine.WindowHost.WindowHeight ||
            Handle.IsEmpty)
        {
            width = (int)engine.WindowHost.WindowWidth;
            height = (int)engine.WindowHost.WindowHeight;
            engine.TextureManager.DisposeTexture(Handle);
            Handle = engine.textureManager.CreateRenderTexture(Math.Max(1, (int)(width * scale)),Math.Max(1, (int)(height * scale)));
        }
    }
    
    public void Dispose()
    {
        engine.TextureManager.DisposeTexture(Handle);
        Handle = default;
    }
}