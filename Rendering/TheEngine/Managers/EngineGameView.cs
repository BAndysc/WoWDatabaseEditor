namespace TheEngine.Managers;

public class EngineGameView : BaseBaseView
{
    private readonly Engine engine;

    /// <summary>True once the "3D" tab has been the active (visible) tab at least once.</summary>
    public bool WasEverVisible { get; private set; }

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
        var rm = engine.renderManager;
        var display = rm.GameDebugReady ? rm.GameDebugTexture : rm.CurrentBackBuffer;
        BeginWindow("3D\0"u8, display?.Handle.ToRawIntPtr() ?? IntPtr.Zero, fullScreen);
        EndWindow();
        if (IsVisible)
            WasEverVisible = true;
    }
}