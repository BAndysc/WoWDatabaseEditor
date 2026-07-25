using System.Diagnostics;
using Avalonia.Input;
using TheAvaloniaOpenGL;

namespace TheEngine;

public class GameRunner
{
    private readonly Engine engine;
    private Stopwatch updateStopwatch = new();
    private Stopwatch renderStopwatch = new Stopwatch();

    public GameRunner(Engine engine)
    {
        this.engine = engine;
    }

    public event Action? SyncInputState;

    public void NextFrame(float deltaTime, IGame game)
    {
        updateStopwatch.Restart();
        engine.FrameCount++;
        IDevice.FrameCount = engine.FrameCount;
        engine.inputManager.PostUpdate();
        engine.inputManager.Update(deltaTime * 1000);
        SyncInputState?.Invoke();

        if (engine.inputManager.Keyboard.JustPressed(Key.R))
        {
            if (engine.Backend is Rendering.GLRenderBackend && engine.Device.device is DebugDevice debug)
            {
                var file = new FileInfo("render_debug.txt");
                File.WriteAllLines(file.FullName, debug.commands);
                Console.WriteLine("Log written to " + file.FullName);
            }
        }

        engine.UpdateGui(deltaTime);
        engine.EngineUi.BeginFrame(deltaTime* 1000);
        engine.ExecuteNextFrameActions();
        game?.Update(deltaTime* 1000);
        engine.renderManager.UpdateTransforms();
        updateStopwatch.Stop();
        engine.statsManager.Counters.UpdateTime.Add(updateStopwatch.Elapsed.TotalMilliseconds);

        // rendering

        engine.TotalTime += deltaTime * 1000;
        engine.statsManager.Counters.FrameTime.Add(deltaTime * 1000);
        renderStopwatch.Restart();
        engine.Backend.BeginFrame();
        engine.renderManager.BeginFrame();
        engine.renderManager.PrepareRendering(0);
        engine.renderManager.RenderOpaque(0);
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom");
        game.Render((float)deltaTime * 1000);
        engine.renderManager.RenderTransparent(0);
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom translucent");
        game.RenderTransparent((float)deltaTime * 1000);
        engine.renderManager.RenderPostProcess();
        engine.Render3DGUI();
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom GUI");
        engine.renderManager.PrepareRenderGui((float)deltaTime * 1000);
        game.RenderGUI((float)deltaTime * 1000);
        engine.RenderGUI();
        engine.renderManager.CommandList.InsertDebugMarker("  Finalize rendering");
        engine.renderManager.FinalizeRendering(0);
        engine.Backend.EndFrame();

        renderStopwatch.Stop();
        engine.statsManager.Counters.TotalRender.Add(renderStopwatch.Elapsed.Milliseconds);
        // if (frameCounter == 160)
        // {
        //     DotnetProfiler.Profiler.Disable();
        //     DotnetProfiler.Profiler.SaveTrace("trace.bin");
        // }
        // Tracy.PInvoke.TracyEmitFrameMark(null);
    }
}