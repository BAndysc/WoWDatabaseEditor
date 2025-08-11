using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TheEngine;
using TheEngine.Utils;

namespace TheEngineTest;

public class GameStandaloneWindow : TheEngineOpenTkWindow
{
    private readonly SingleThreadSynchronizationContext ctx;

    public GameStandaloneWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IGame game, SingleThreadSynchronizationContext ctx) :
        base(gameWindowSettings, nativeWindowSettings, game)
    {
        this.ctx = ctx;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        ctx.ExecuteTasks();
    }
}