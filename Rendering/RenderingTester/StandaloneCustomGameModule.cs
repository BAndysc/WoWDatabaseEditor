using TheEngine.Coroutines;
using System.Collections;
using System.Windows.Input;
using Avalonia.Input;
using ImGuiNET;
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
using MouseButton = TheEngine.Input.MouseButton;

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
    private readonly ITextureManager textureManager;
    private readonly IMeshManager meshManager;
    private readonly ModuleManager moduleManager;
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
        ModuleManager moduleManager,
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
        this.moduleManager = moduleManager;
        this.archetypes = archetypes;
        this.mdxManager = mdxManager;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        //gameContext.SetMap(571);
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
    
    public List<(string, ICommand, object?)>? GenerateContextMenu()
    {
        List<(string, ICommand, object?)>? allItems = null;
        moduleManager.ForEach(mod =>
        {
            var items = mod.GenerateContextMenu();
            if (items != null)
            {
                allItems ??= new List<(string, ICommand, object?)>();
                allItems.AddRange(items);
            }
        });
        return allItems == null || allItems.Count == 0 ? null : allItems;
    }

    private List<(string, ICommand, object?)>? currentMenu;
    
    public void RenderGUI()
    {
        if (inputManager.Mouse.HasJustClicked(MouseButton.Right))
        {
            currentMenu = GenerateContextMenu();
            ImGui.OpenPopup("contextmenu");
        }

        if (currentMenu != null)
        {
            if (ImGui.BeginPopupContextItem("contextmenu"))
            {
                foreach (var option in currentMenu)
                {
                    if (option.Item1 == "-")
                        ImGui.Text("----");
                    else
                    {
                        if (ImGui.Selectable(option.Item1))
                        {
                            option.Item2.Execute(option.Item3);
                            ImGui.CloseCurrentPopup();
                        }   
                    }
                }
                ImGui.EndPopup();
            }
            else
                currentMenu = null;
        }
        
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
