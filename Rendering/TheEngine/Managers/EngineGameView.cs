namespace TheEngine.Managers;

public class EngineGameView : BaseBaseView
{
    private readonly Engine engine;

    public EngineGameView(Engine engine)
    {
        this.engine = engine;
    }

    public void Draw(float delta)
    {
        bool fullScreen = false;
#if ENGINE_RELEASE
        fullScreen = true;
#endif
        BeginWindow("3D\0"u8, engine.renderManager.CurrentBackBuffer?.Handle.ToRawIntPtr() ?? IntPtr.Zero, fullScreen);
        EndWindow();
    }
}