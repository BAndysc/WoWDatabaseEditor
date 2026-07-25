using Hexa.NET.ImGui;
using JetBrains.Profiler.Api;
using TheEngine;
using TheEngine.Coroutines;
using TheEngine.Input;
using TheEngine.Interfaces;
using IInputManager = TheEngine.Interfaces.IInputManager;
using Vector2 = System.Numerics.Vector2;

namespace WDE.MapRenderer.Modules;

public class DebugInfoGameModule : IGameModule
{
    private readonly IUIManager uiManager;
    private readonly CoroutineManager coroutineManager;
    private readonly IStatsManager statsManager;
    private readonly IInputManager inputManager;
    private readonly Engine engine;
    public object? ViewModel => null;

    public DebugInfoGameModule(IUIManager uiManager,
        CoroutineManager coroutineManager,
        IStatsManager statsManager,
        IInputManager inputManager,
        Engine engine)
    {
        this.uiManager = uiManager;
        this.coroutineManager = coroutineManager;
        this.statsManager = statsManager;
        this.inputManager = inputManager;
        this.engine = engine;
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

        var gameView = engine.GameView.ViewRect;

        // Bottom-right stats: styled like the spawn editor's inspector panel - a narrow rounded
        // panel with a collapse arrow; collapsed it shrinks to a rounded "Stats" tab. The fps lives
        // in the tab/header (no separate FPS chip - the bottom-left corner belongs to the camera
        // coordinates box).
        if (statsCollapsed)
            DrawCollapsedStatsTab(gameView.Right, gameView.Bottom, fps);
        else
            DrawStatsPanel(gameView.Right, gameView.Bottom, fps);
    }

    private bool statsCollapsed = true;
    private const float StatsPanelWidth = 340f;
    private const float Margin = 10f;

    private void DrawStatsPanel(float right, float bottom, float fps)
    {
        ref var counters = ref statsManager.Counters;
        ref var stats = ref statsManager.RenderStats;
        float w = statsManager.PixelSize.X;
        float h = statsManager.PixelSize.Y;

        ImGui.SetNextWindowPos(new Vector2(right - Margin, bottom - Margin), ImGuiCond.Always, new Vector2(1.0f, 1.0f));
        ImGui.SetNextWindowSizeConstraints(new Vector2(StatsPanelWidth, 0), new Vector2(StatsPanelWidth, float.MaxValue));
        ImGui.SetNextWindowBgAlpha(0.85f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6f);
        if (ImGui.Begin("Stats", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                       ImGuiWindowFlags.NoMove | ImGuiWindowFlags.AlwaysAutoResize |
                       ImGuiWindowFlags.NoSavedSettings))
        {
            // header: title + fps + right-aligned collapse arrow (same layout as the inspector panel)
            ImGui.TextDisabled($"Stats · {fps:0.0} fps");
            ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.GetFrameHeight() - 8);
            if (ImGui.ArrowButton("##collapsestats", ImGuiDir.Right))
                statsCollapsed = true;
            ImGui.Separator();

            ImGui.Text($"[{w:0}x{h:0}]");
            ImGui.Text($"Total frame time: {counters.FrameTime.Average:0.00} ms");
            ImGui.Text($" - Update time: {counters.UpdateTime.Average:0.00} ms");
            ImGui.Text($" - Render time: {counters.TotalRender.Average:0.00} ms");
            ImGui.Text($"   - Wait (fence/acquire): {counters.GpuFenceWait.Average:0.00}/{counters.GpuAcquire.Average:0.00} ms");
            ImGui.Text($"   - Bounds: {counters.BoundsCalc.Average:0.00}ms");
            ImGui.Text($"   - Culling: {counters.Culling.Average:0.00}ms");
            ImGui.Text($"   - Sorting: {counters.Sorting.Average:0.00}ms");
            ImGui.Text($"   - Drawing: {counters.Drawing.Average:0.00}ms");
            ImGui.Text($"   - Begin/prep: {counters.RenderBegin.Average:0.00}/{counters.RenderPrepare.Average:0.00} ms");
            ImGui.Text($"   - Opaque/game/transp: {counters.RenderOpaquePhase.Average:0.00}/{counters.RenderGameCustom.Average:0.00}/{counters.RenderTransparentPhase.Average:0.00} ms");
            ImGui.Text($"   - Post/gui/final/end: {counters.RenderPostProcessPhase.Average:0.00}/{counters.RenderGui.Average:0.00}/{counters.RenderFinalize.Average:0.00}/{counters.RenderEnd.Average:0.00} ms");
            ImGui.Text($"   - Present time: {counters.PresentTime.Average:0.00} ms");
            ImGui.Text("Shaders: " + stats.ShaderSwitches);
            ImGui.Text($"Materials: " + stats.MaterialActivations);
            ImGui.Text($"Meshes: " + stats.MeshSwitches);
            ImGui.Text($"Batches: " + (stats.NonInstancedDraws + stats.InstancedDraws));
            ImGui.Text($"Batches saved by instancing: " + stats.InstancedDrawSaved);
            ImGui.Text($"Tris: " + stats.TrianglesDrawn);
            ImGui.Text($"Set1 cache hits/misses: {stats.Set1CacheHits}/{stats.Set1CacheMisses}");
            ImGui.Text($"Texture size (MB): {statsManager.TextureBytes / 1024 / 1024:0.00}");
            ImGui.Text($"Entities (MB): {statsManager.EntitiesUnmanagedBytes / 1024 / 1024:0.00}");
            ImGui.Text($"Buffers (MB): {statsManager.BufferBytes / 1024 / 1024:0.00}");
            ImGui.Text($"GPU alloc/free per frame: {statsManager.GpuAllocationsPerFrame}/{statsManager.GpuFreesPerFrame}");
            ImGui.Text($"GPU device blocks alloc/free: {statsManager.GpuDeviceAllocationsPerFrame}/{statsManager.GpuDeviceFreesPerFrame}");
        }
        ImGui.End();
        ImGui.PopStyleVar();
    }

    // The same rounded labeled tab the inspector panel collapses into, hugging the bottom-right
    // corner. Shows the fps so the most-wanted stat survives collapsing.
    private string collapsedLabel = "";
    private int collapsedLabelFps = -1;

    private void DrawCollapsedStatsTab(float right, float bottom, float fps)
    {
        // rebuilt only when the rounded fps changes - this draws every frame
        if ((int)fps != collapsedLabelFps)
        {
            collapsedLabelFps = (int)fps;
            collapsedLabel = $"Stats · {collapsedLabelFps} fps";
        }
        string label = collapsedLabel;
        var textSize = ImGui.CalcTextSize(label);
        var pad = new Vector2(10, 5);
        const float arrowSpace = 12;
        var size = new Vector2(textSize.X + arrowSpace + pad.X * 2, textSize.Y + pad.Y * 2);

        ImGui.SetNextWindowPos(new Vector2(right - Margin, bottom - Margin), ImGuiCond.Always, new Vector2(1.0f, 1.0f));
        ImGui.SetNextWindowSize(size, ImGuiCond.Always);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        if (ImGui.Begin("Stats", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize |
                       ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoSavedSettings |
                       ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar))
        {
            var min = ImGui.GetWindowPos();
            var max = min + size;
            if (ImGui.InvisibleButton("##expandstats", size))
                statsCollapsed = false;
            bool hovered = ImGui.IsItemHovered();
            if (hovered)
                ImGui.SetTooltip("Show render stats");

            var dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(min, max, ImGui.GetColorU32(hovered ? ImGuiCol.ButtonHovered : ImGuiCol.WindowBg, hovered ? 1f : 0.85f), 6f);
            dl.AddRect(min, max, ImGui.GetColorU32(ImGuiCol.Border, 0.6f), 6f);
            // ◂ expand arrow + label, like the inspector's collapsed tab
            float cy = (min.Y + max.Y) * 0.5f;
            float ax = min.X + pad.X;
            dl.AddTriangleFilled(new Vector2(ax, cy), new Vector2(ax + 7, cy - 5), new Vector2(ax + 7, cy + 5),
                ImGui.GetColorU32(ImGuiCol.Text, 0.9f));
            dl.AddText(new Vector2(ax + arrowSpace, min.Y + pad.Y), ImGui.GetColorU32(ImGuiCol.Text, 0.95f), label);
        }
        ImGui.End();
        ImGui.PopStyleVar(2);
    }
}
