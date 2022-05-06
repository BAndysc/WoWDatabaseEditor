using TheEngine.Coroutines;
using System.Collections;
using TheEngine.Entities;
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;

namespace RenderingTester;

public class StandaloneCustomGameModule : IGameModule 
{
    private readonly IUIManager uiManager;
    private readonly CoroutineManager coroutineManager;
    private readonly IStatsManager statsManager;
    private readonly CameraManager cameraManager;
    private readonly IGameContext gameContext;
    private readonly IRenderManager renderManager;
    private readonly MdxManager mdxManager;
    public object? ViewModel => null;

    public StandaloneCustomGameModule(IUIManager uiManager,
        CoroutineManager coroutineManager,
        IStatsManager statsManager,
        CameraManager cameraManager,
        IGameContext gameContext,
        IRenderManager renderManager,
        MdxManager mdxManager)
    {
        this.uiManager = uiManager;
        this.coroutineManager = coroutineManager;
        this.statsManager = statsManager;
        this.cameraManager = cameraManager;
        this.gameContext = gameContext;
        this.renderManager = renderManager;
        this.mdxManager = mdxManager;
    }
    
    public void Dispose()
    {
    }

    public void Initialize()
    {
        //cameraManager.Relocate(new Vector3());
        //gameContext.StartCoroutine(LoadModel());
    }

    private IEnumerator LoadModel()
    {
        var task = new TaskCompletionSource<MdxManager.MdxInstance?>();
        //"world\\generic\\activedoodads\\trollchest\\trollchest.m2"
        yield return mdxManager.LoadM2Mesh("creature\\rocketchicken\\rocketchicken.m2", task);//"creature\\rocketchicken\\rocketchicken.m2", task);
        int i = 0;
        Transform transform = new();
        transform.Scale *= 5;
        var materials = task.Task.Result.materials;
        foreach (var pair in materials)
        {
            renderManager.RegisterStaticRenderer(task.Task.Result.mesh.Handle, pair, i++, transform);
        }
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
        ui.Text("calibri", $"{coroutineManager.PendingCoroutines} active tasks", 14, Vector4.One);
    }
}
