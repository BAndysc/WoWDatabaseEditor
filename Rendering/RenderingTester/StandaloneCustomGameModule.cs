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
using TheEngine.Interfaces;
using TheMaths;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace RenderingTester;

public class StandaloneCustomGameModule : IGameModule 
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
        this.meshManager = meshManager;
        this.archetypes = archetypes;
        this.mdxManager = mdxManager;
    }

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
        outlineMaterial.BlendingEnabled = true;
        outlineMaterial.SourceBlending = Blending.SrcAlpha;
        outlineMaterial.DestinationBlending = Blending.OneMinusSrcAlpha;
        renderManager.AddPostprocess(outlineMaterial);
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

    private RenderTexture? RT;
    private int prevW, prevH;
    
    public void Render()
    {
        if (RT == null || prevW != (int)engine.WindowHost.WindowWidth || prevH != (int)engine.WindowHost.WindowHeight)
        {
            prevW = (int)engine.WindowHost.WindowWidth;
            prevH = (int)engine.WindowHost.WindowHeight;
            RT?.Dispose();
            RT = engine.CreateRenderTexture(prevW / 4, prevH / 4);
            outlineMaterial.SetTexture("outlineTex", RT);
        }
        
        renderManager.ActivateRenderTexture(RT);
        RT.Clear(0, 0, 0, 0);
        
        archetypes.StaticM2WorldObjectAnimatedArchetype.ForEach<LocalToWorld, MeshRenderer, MaterialInstanceRenderData>(
        (itr, start, end, localToWorldAccess, rendererAccess, materialInstanceData) =>
        {
            for (int i = start; i < end; ++i)
            {
                var localToWorld = localToWorldAccess[i];
                var renderer = rendererAccess[i];
                var instanceData = materialInstanceData[i];

                var oldMaterial = materialManager.GetMaterialByHandle(renderer.MaterialHandle);
                replacementMaterial.SetTexture("texture1", oldMaterial.GetTexture("texture1"));
                replacementMaterial.SetBuffer("boneMatrices", instanceData.GetBuffer("boneMatrices")!);
                
                renderManager.Render(renderer.MeshHandle, replacementMaterial.Handle, renderer.SubMeshId, localToWorld.Matrix, localToWorld.Inverse);
            }
        });

        renderManager.ActivateDefaultRenderTexture();
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
}
