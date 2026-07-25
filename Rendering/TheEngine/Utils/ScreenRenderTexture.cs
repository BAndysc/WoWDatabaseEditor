using TheEngine.Handles;
using TheEngine.Interfaces;

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
    
    public ITexture? Texure { get; private set; }

    public void Update()
    {
        if (width != (int)engine.gameView.ViewRect.Width ||
            height != (int)engine.gameView.ViewRect.Height ||
            Texure == null)
        {
            width = (int)engine.gameView.ViewRect.Width;
            height = (int)engine.gameView.ViewRect.Height;
            engine.TextureManager.DisposeTexture(Texure);
            Texure = engine.textureManager.CreateRenderTexture(Math.Max(1, (int)(width * scale)),Math.Max(1, (int)(height * scale)));
        }
    }
    
    public void Dispose()
    {
        engine.TextureManager.DisposeTexture(Texure);
        Texure = default;
    }
}