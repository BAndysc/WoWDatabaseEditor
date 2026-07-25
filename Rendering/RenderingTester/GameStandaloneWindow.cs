using JetBrains.Annotations;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using TheEngine;
using TheEngine.Utils;
using WDE.Common.Services;

namespace RenderingTester;

public class GameStandaloneWindow : TheEngineVulkanOpenTkWindow, IClipboardService
{
    private readonly MainThread mainThreadImpl;

    public GameStandaloneWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IGame game, MainThread mainThreadImpl) :
        base(nativeWindowSettings, game)
    {
        this.mainThreadImpl = mainThreadImpl;
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