using TheEngine.Coroutines;
using System.Collections;
using Avalonia.Input;
using JetBrains.Profiler.Api;
using OpenTK.Platform.Windows;
using TheAvaloniaOpenGL.Resources;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Entities;
using TheEngine.Handles;
using TheEngine.Interfaces;
using TheEngine.Utils;
using TheMaths;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace RenderingTester;

public class StandaloneCustomGameModule : IGameModule, IPostProcess
{
    private readonly IUIManager uiManager;
    private readonly CoroutineManager coroutineManager;
    private readonly IStatsManager statsManager;
    private readonly CameraManager cameraManager;
    private readonly IGameContext gameContext;
    private readonly IRenderManager renderManager;
    private readonly IInputManager inputManager;
    private readonly Engine engine;
    private readonly IMaterialManager materialManager;
    private readonly ITextureManager textureManager;
    private readonly IMeshManager meshManager;
    private readonly Archetypes archetypes;
    private readonly MdxManager mdxManager;
    public object? ViewModel => null;

    public StandaloneCustomGameModule(IUIManager uiManager,
        CoroutineManager coroutineManager,
        IStatsManager statsManager,
        CameraManager cameraManager,
        IGameContext gameContext,
        IRenderManager renderManager,
        IInputManager inputManager,
        Engine engine,
        IMaterialManager materialManager,
        ITextureManager textureManager,
        IMeshManager meshManager,
        Archetypes archetypes,
        MdxManager mdxManager)
    {
        this.uiManager = uiManager;
        this.coroutineManager = coroutineManager;
        this.statsManager = statsManager;
        this.cameraManager = cameraManager;
        this.gameContext = gameContext;
        this.renderManager = renderManager;
        this.inputManager = inputManager;
        this.engine = engine;
        this.materialManager = materialManager;
        this.textureManager = textureManager;
        this.meshManager = meshManager;
        this.archetypes = archetypes;
        this.mdxManager = mdxManager;
    }

    private Material blurMaterial = null!;
    private Material replacementMaterial = null!;
    private Material outlineMaterial = null!;
    
    public void Dispose()
    {
    }

    public void Initialize()
    {
        //gameContext.SetMap(571);
        replacementMaterial = materialManager.CreateMaterial("data/unlit_flat_m2.json");
        replacementMaterial.SetUniform("mesh_color", new Vector4(1, 0, 0, 1));

        outlineMaterial = materialManager.CreateMaterial("data/outline.json");
        outlineMaterial.BlendingEnabled = false;
        outlineMaterial.SourceBlending = Blending.One;
        outlineMaterial.DestinationBlending = Blending.Zero;
        outlineMaterial.DepthTesting = DepthCompare.Always;
        
        blurMaterial = materialManager.CreateMaterial("data/blur.json");
        blurMaterial.BlendingEnabled = true;
        blurMaterial.SourceBlending = Blending.SrcAlpha;
        blurMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
        blurMaterial.DepthTesting = DepthCompare.Always;
        blurMaterial.SetUniform("blurSize", 0.125f/4);
        //blurMaterial.SetUniformInt("horizontalPass", 0);
        //blurMaterial.SetUniform("sigma", 4);

        RT = new ScreenRenderTexture(engine);
        RT_downscaled = new ScreenRenderTexture(engine, 0.25f);
        
        renderManager.AddPostprocess(this);
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

    private ScreenRenderTexture RT = null!;
    private ScreenRenderTexture RT_downscaled = null!;
    
    public void Render()
    {
        RT.Update();
        RT_downscaled.Update();
        outlineMaterial.SetTexture("outlineTex", RT_downscaled);
        outlineMaterial.SetTexture("outlineTexUnBlurred", RT);
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
        ui2.Text("calibri", $"   - Drawing: {counters.Drawing.Average:0.00}ms", 12, Vector4.One);
        ui2.Text("calibri", $"   - Present time: {counters.PresentTime.Average:0.00} ms", 12, Vector4.One);
        ui2.Text("calibri", "Shaders: " + stats.ShaderSwitches, 12, Vector4.One);
        ui2.Text("calibri", $"Materials: " + stats.MaterialActivations, 12, Vector4.One);
        ui2.Text("calibri", $"Meshes: " + stats.MeshSwitches, 12, Vector4.One);
        ui2.Text("calibri", $"Batches: " + (stats.NonInstancedDraws + stats.InstancedDraws), 12, Vector4.One);
        ui2.Text("calibri", $"Batches saved by instancing: " + stats.InstancedDrawSaved, 12, Vector4.One);
        ui2.Text("calibri", $"Tris: " + stats.TrianglesDrawn, 12, Vector4.One);
    }

    public void RenderPostprocess(IRenderManager context, TextureHandle currentImage)
    {
        outlineMaterial.SetTexture("_MainTex", currentImage);
        context.RenderFullscreenPlane(outlineMaterial);
    }
}
