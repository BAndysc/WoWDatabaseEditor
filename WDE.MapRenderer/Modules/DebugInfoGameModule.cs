using Avalonia.Input;
using ImGuiNET;
using JetBrains.Profiler.Api;
using TheEngine.Coroutines;
using TheEngine.Interfaces;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace WDE.MapRenderer.Modules;

public class DebugInfoGameModule : IGameModule
{
    private readonly IUIManager uiManager;
    private readonly CoroutineManager coroutineManager;
    private readonly IStatsManager statsManager;
    private readonly IInputManager inputManager;
    public object? ViewModel => null;

    public DebugInfoGameModule(IUIManager uiManager,
        CoroutineManager coroutineManager,
        IStatsManager statsManager,
        IInputManager inputManager)
    {
        this.uiManager = uiManager;
        this.coroutineManager = coroutineManager;
        this.statsManager = statsManager;
        this.inputManager = inputManager;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
    }

    private bool profiling = false;

    public void Update(float delta)
    {
        if (profiling)
        {
            MeasureProfiler.StopCollectingData();
            MeasureProfiler.SaveData();
            profiling = false;
        }
        if (inputManager.Keyboard.JustPressed(Key.P))
        {
            MeasureProfiler.StartCollectingData();
            profiling = true;
        }
    }

    public void Render(float delta)
    {
    }
    
    public void RenderGUI()
    {
        var fps = 1000 / statsManager.Counters.FrameTime.Average;

        // Top-left FPS window
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(10, 10), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(1.0f);
        if (ImGui.Begin("FPS", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                       ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                       ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text($"{fps:0.00}");
            ImGui.Text($"{coroutineManager.PendingCoroutines} active tasks");
            ImGui.End();
        }

        // Top-right stats window
        var viewport = ImGui.GetIO().DisplaySize / ImGui.GetIO().DisplayFramebufferScale;
        ref var counters = ref statsManager.Counters;
        ref var stats = ref statsManager.RenderStats;
        float w = statsManager.PixelSize.X;
        float h = statsManager.PixelSize.Y;

        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.X - 10, viewport.Y - 10), ImGuiCond.Always, new System.Numerics.Vector2(1.0f, 1.0f));
        ImGui.SetNextWindowBgAlpha(0.5f);
        if (ImGui.Begin("Stats", ImGuiWindowFlags.NoResize |
                       ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                       ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text($"[{w:0}x{h:0}]");
            ImGui.Text($"Total frame time: {counters.FrameTime.Average:0.00} ms");
            ImGui.Text($" - Update time: {counters.UpdateTime.Average:0.00} ms");
            ImGui.Text($" - Render time: {counters.TotalRender.Average:0.00} ms");
            ImGui.Text($"   - Bounds: {counters.BoundsCalc.Average:0.00}ms");
            ImGui.Text($"   - Culling: {counters.Culling.Average:0.00}ms");
            ImGui.Text($"   - Sorting: {counters.Sorting.Average:0.00}ms");
            ImGui.Text($"   - Drawing: {counters.Drawing.Average:0.00}ms");
            ImGui.Text($"   - Present time: {counters.PresentTime.Average:0.00} ms");
            ImGui.Text("Shaders: " + stats.ShaderSwitches);
            ImGui.Text($"Materials: " + stats.MaterialActivations);
            ImGui.Text($"Meshes: " + stats.MeshSwitches);
            ImGui.Text($"Batches: " + (stats.NonInstancedDraws + stats.InstancedDraws));
            ImGui.Text($"Batches saved by instancing: " + stats.InstancedDrawSaved);
            ImGui.Text($"Tris: " + stats.TrianglesDrawn);
            ImGui.Text($"Texture size (MB): {statsManager.TextureBytes / 1024 / 1024:0.00}");
            ImGui.Text($"Entities (MB): {statsManager.EntitiesUnmanagedBytes / 1024 / 1024:0.00}");
            ImGui.Text($"Buffers (MB): {statsManager.BufferBytes / 1024 / 1024:0.00}");
        }
        ImGui.End();
    }
}
