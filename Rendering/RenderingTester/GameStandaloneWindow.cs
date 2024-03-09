using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TheEngine;
using WDE.Common.Services;

namespace RenderingTester;

public class GameStandaloneWindow : TheEngineOpenTkWindow, IClipboardService
{
    private readonly MainThread mainThreadImpl;
    private readonly SingleThreadSynchronizationContext ctx;

    public GameStandaloneWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IGame game, MainThread mainThreadImpl, SingleThreadSynchronizationContext ctx) :
        base(gameWindowSettings, nativeWindowSettings, game)
    {
        this.mainThreadImpl = mainThreadImpl;
        this.ctx = ctx;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        ctx.ExecuteTasks();
        mainThreadImpl.Tick(TimeSpan.FromSeconds(args.Time));
    }

    public Task<string?> GetText()
    {
        return Task.FromResult<string?>(ClipboardString);
    }

    public void SetText(string text)
    {
        ClipboardString = text;
    }
}