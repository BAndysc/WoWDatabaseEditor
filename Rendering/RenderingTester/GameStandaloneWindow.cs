using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TheEngine;

namespace RenderingTester;

public class GameStandaloneWindow : TheEngineOpenTkWindow
{
    private readonly MainThread mainThreadImpl;

    public GameStandaloneWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IGame game, MainThread mainThreadImpl) :
        base(gameWindowSettings, nativeWindowSettings, game)
    {
        this.mainThreadImpl = mainThreadImpl;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        mainThreadImpl.Tick(TimeSpan.FromSeconds(args.Time));
    }
}