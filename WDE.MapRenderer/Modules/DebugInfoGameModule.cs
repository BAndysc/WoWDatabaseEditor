using Avalonia.Input;
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
        using var ui = uiManager.BeginImmediateDrawRel(1, 0, 1, 0);
        ui.BeginVerticalBox(new Vector4(0, 0, 0, 1), 2);
        ui.Text("calibri", $"{fps:0.00}", 14, Vector4.One);
        ui.Text("calibri", $"{coroutineManager.PendingCoroutines} active tasks", 14, Vector4.One);

        using var ui2 = uiManager.BeginImmediateDrawRel(1, 1, 1, 1);
        ref var counters = ref statsManager.Counters;
        ref var stats = ref statsManager.RenderStats;
        float w = statsManager.PixelSize.X;
        float h = statsManager.PixelSize.Y;
        
        ui2.BeginVerticalBox(new Vector4(0, 0, 0, 0.5f), 2);
        ui2.Text("calibri", $"[{w:0}x{h:0}]", 12, Vector4.One);
        ui2.Text("calibri", $"Total frame time: {counters.FrameTime.Average:0.00} ms", 12, Vector4.One);
        ui2.Text("calibri", $" - Update time: {counters.UpdateTime.Average:0.00} ms", 12, Vector4.One);
        ui2.Text("calibri", $" - Render time: {counters.TotalRender.Average:0.00} ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Bounds: {counters.BoundsCalc.Average:0.00}ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Culling: {counters.Culling.Average:0.00}ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Sorting: {counters.Sorting.Average:0.00}ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Drawing: {counters.Drawing.Average:0.00}ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Present time: {counters.PresentTime.Average:0.00} ms", 12, Vector4.One);
        ui2.Text("calibri", "Shaders: " + stats.ShaderSwitches, 12, Vector4.One);
        ui2.Text("calibri", $"Materials: " + stats.MaterialActivations, 12, Vector4.One);
        ui2.Text("calibri", $"Meshes: " + stats.MeshSwitches, 12, Vector4.One);
        ui2.Text("calibri", $"Batches: " + (stats.NonInstancedDraws + stats.InstancedDraws), 12, Vector4.One);
        ui2.Text("calibri", $"Batches saved by instancing: " + stats.InstancedDrawSaved, 12, Vector4.One);
        ui2.Text("calibri", $"Tris: " + stats.TrianglesDrawn, 12, Vector4.One);
    }
}
