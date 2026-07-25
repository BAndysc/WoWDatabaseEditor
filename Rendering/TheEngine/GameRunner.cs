using System.Diagnostics;
using TheEngine;
using TheEngine.Utils;

namespace TheEngine;

public class GameRunner
{
    private readonly Engine engine;
    private TheEngineSynchronizationContext sc;
    private bool gameInitialized;

    public GameRunner(Engine engine)
    {
        this.engine = engine;
        sc = new TheEngineSynchronizationContext(engine, Environment.CurrentManagedThreadId);
    }

    public event Action? SyncInputState;

    /// <summary>Runs work still queued on the game-loop synchronization context. Call during
    /// shutdown after the last frame - once NextFrame stops being called, queued continuations
    /// would otherwise never run and their awaiters would hang forever.</summary>
    public void DrainPendingWork()
    {
        var oldSC = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(sc);
        try
        {
            sc.ExecuteTasks();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(oldSC);
        }
    }

    public bool NextFrame(float deltaTime, IGame game)
    {
        var oldSC = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(sc);
        try
        {

            if (!gameInitialized)
            {
                gameInitialized = true;
                if (!game.Initialize(engine!))
                {
                    return false;
                }
            }

            if (game == null)
            {
                return false;
            }
            NextFrameImpl(deltaTime, game);
            return true;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(oldSC);
        }
    }

    private void NextFrameImpl(float deltaTime, IGame game)
    {
        // FOR A TEST, used to be above engine.renderManager.BeginFrame();
        var timer = CheapStopWatch.StartNew();
        engine.BeginFrame();
        var beginElapsed = timer.Elapsed;

        timer = CheapStopWatch.StartNew();
        engine.FrameCount++;
        engine.inputManager.PostUpdate();
        engine.inputManager.Update(deltaTime * 1000);
        SyncInputState?.Invoke();

        engine.UpdateGui(deltaTime);
        engine.EngineUi.BeginFrame(deltaTime* 1000);
        sc.ExecuteTasks();
        engine.ExecuteNextFrameActions();
        game?.Update(deltaTime* 1000);
        engine.physicsManager.Step(deltaTime * 1000);
        engine.renderManager.UpdateTransforms();
        var updateElapsed = timer.Elapsed;
        engine.statsManager.Counters.UpdateTime.Add(updateElapsed.TotalMilliseconds);

        // rendering

        engine.TotalTime += deltaTime * 1000;
        engine.statsManager.Counters.FrameTime.Add(deltaTime * 1000);
        var renderStopwatch = CheapStopWatch.StartNew();
        engine.renderManager.BeginFrame();
        timer = CheapStopWatch.StartNew();
        engine.renderManager.PrepareRendering(0);
        var prepElapsed = timer.Elapsed;
        timer = CheapStopWatch.StartNew();
        engine.renderManager.RenderOpaque(0);
        var opaqueElapsed = timer.Elapsed;
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom");
        timer = CheapStopWatch.StartNew();
        game.Render((float)deltaTime * 1000);
        var gameElapsed = timer.Elapsed;
        timer = CheapStopWatch.StartNew();
        engine.renderManager.RenderTransparent(0);
        var transpElapsed = timer.Elapsed;
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom translucent");
        timer = CheapStopWatch.StartNew();
        game.RenderTransparent((float)deltaTime * 1000);
        var gameTranspElapsed = timer.Elapsed;
        // when a debug view is selected, render it into the per-view debug textures (depth/shadow/grids
        // are populated by now); the views display those instead of the final image
        engine.renderManager.RenderDebugViews();
        timer = CheapStopWatch.StartNew();
        engine.renderManager.RenderPostProcess();
        engine.Render3DGUI();
        var ppElapsed = timer.Elapsed;
        engine.renderManager.CommandList.InsertDebugMarker("  Rendering Game custom GUI");
        timer = CheapStopWatch.StartNew();
        engine.renderManager.PrepareRenderGui((float)deltaTime * 1000);
        game.RenderGUI((float)deltaTime * 1000);
        engine.RenderGUI();
        var guiElapsed = timer.Elapsed;
        engine.renderManager.CommandList.InsertDebugMarker("  Finalize rendering");
        timer = CheapStopWatch.StartNew();
        engine.renderManager.FinalizeRendering(0);
        var finalizeElapsed = timer.Elapsed;
        timer = CheapStopWatch.StartNew();
        engine.EndFrame();
        var endElapsed = timer.Elapsed;

        engine.statsManager.Counters.TotalRender.Add(renderStopwatch.Elapsed.TotalMilliseconds);
        // the BeginFrame fence wait + swapchain acquire are part of TotalRender; expose them so it's
        // clear how much of the render time is idle-waiting rather than CPU/GPU work
        engine.statsManager.Counters.GpuFenceWait.Add(engine.Backend.LastFenceWaitMs);
        engine.statsManager.Counters.GpuAcquire.Add(engine.Backend.LastAcquireMs);
        engine.statsManager.Counters.GpuPresentWait.Add(engine.Backend.LastPresentWaitMs);
        // whole-frame phase breakdown (begin includes the fence/acquire wait above)
        engine.statsManager.Counters.RenderBegin.Add(beginElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderPrepare.Add(prepElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderOpaquePhase.Add(opaqueElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderTransparentPhase.Add(transpElapsed.TotalMilliseconds);
        // the game's own draw callbacks (map chunks, M2s, ...) - previously untimed, between the buckets
        engine.statsManager.Counters.RenderGameCustom.Add(gameElapsed.TotalMilliseconds + gameTranspElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderPostProcessPhase.Add(ppElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderGui.Add(guiElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderFinalize.Add(finalizeElapsed.TotalMilliseconds);
        engine.statsManager.Counters.RenderEnd.Add(endElapsed.TotalMilliseconds);

        if (Environment.GetEnvironmentVariable("THEENGINE_PROFILE") == "1" && (engine.FrameCount % 120) == 0)
        {
            var c = engine.statsManager.Counters;
            var s = engine.statsManager.RenderStats;
            Console.WriteLine($"[prof] frame={c.FrameTime.Average:0.0}ms update={c.UpdateTime.Average:0.0} render={c.TotalRender.Average:0.0} GPU={engine.Backend.LastGpuMs:0.0} | begin={beginElapsed.TotalMilliseconds:0.0}(fence={engine.Backend.LastFenceWaitMs:0.0} presentwait={engine.Backend.LastPresentWaitMs:0.0} acquire={engine.Backend.LastAcquireMs:0.0} qreadback={engine.Backend.LastQueryReadbackMs:0.0} destroy={engine.Backend.LastDestroyMs:0.0} ndestroy={engine.Backend.LastDestroyedCount} qlen={engine.Backend.PendingDestroyQueueLength}) prep={prepElapsed.TotalMilliseconds:0.0} opaque={opaqueElapsed.TotalMilliseconds:0.0} transp={transpElapsed.TotalMilliseconds:0.0} post={ppElapsed.TotalMilliseconds:0.0} gui={guiElapsed.TotalMilliseconds:0.0} finalize={finalizeElapsed.TotalMilliseconds:0.0} end={endElapsed.TotalMilliseconds:0.0} | batches={s.NonInstancedDraws + s.InstancedDraws} set1 {s.Set1CacheHits}/{s.Set1CacheMisses} views[game={engine.GameView.IsVisible} scene={engine.SceneView.IsVisible}]");
        }
        // if (frameCounter == 160)
        // {
        //     DotnetProfiler.Profiler.Disable();
        //     DotnetProfiler.Profiler.SaveTrace("trace.bin");
        // }
        // Tracy.PInvoke.TracyEmitFrameMark(null);
    }
}