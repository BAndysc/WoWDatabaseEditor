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
    }

    public void Update(float delta)
    {
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
    }
}
