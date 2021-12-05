using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer;

namespace RenderingTester;

public class StandaloneCustomGameModule : IGameModule 
{
    private readonly IUIManager uiManager;
    private readonly IStatsManager statsManager;
    public object? ViewModel => null;

    public StandaloneCustomGameModule(IUIManager uiManager,
        IStatsManager statsManager)
    {
        this.uiManager = uiManager;
        this.statsManager = statsManager;
    }
    
    public void Dispose()
    {
    }

    public void Initialize()
    {
    }

    public void Update(float delta)
    {
    }

    public void Render()
    {
    }
    
    public void RenderGUI()
    {
        var fps = 1000 / statsManager.Counters.FrameTime.Average;
        using var ui = uiManager.BeginImmediateDrawRel(1, 0, 1, 0);
        ui.BeginVerticalBox(new Vector4(0, 0, 0, 1), 2);
        ui.Text("calibri", $"{fps:0.00}", 14, Vector4.One);
    }
}