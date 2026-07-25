using System.Collections;
using System.Windows.Input;
using Avalonia.Animation;
using ImGuiNET;
using Prism.Ioc;
using TheEngine;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Input;
using TheEngine.Interfaces;
using TheEngine.Structures;
using TheEngine.Utils;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;
using WDE.MpqReader.Structures;

namespace WDE.MapSpawns.Rendering;

public class SpawnViewer : IGameModule
{
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly IGameContext gameContext;
    private readonly IEntityManager entityManager;
    private readonly IRenderManager renderManager;
    private readonly IGameEventService gameEventService;
    private readonly IGamePhaseService gamePhaseService;
    private readonly IInputManager inputManager;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly IMainThread mainThread;
    private readonly MdxManager mdxManager;
    private readonly AnimationSystem animationSystem;
    private readonly Engine engine;
    private readonly SpawnInfoBoxWindow infoBoxWindow;
    public object? ViewModel => null;

    private HighlightPostProcess postProcess;
    
    private List<System.IDisposable> disposables = new();

    private SpawnDragger spawnDragger;
    private readonly SpawnContextMenu spawnContextMenu;

    private RenderLayer renderLayer;

    public SpawnViewer(ICachedDatabaseProvider databaseProvider,
        ISpawnsContainer spawnsContainer,
        IGameContext gameContext,
        IEntityManager entityManager,
        IRenderManager renderManager,
        IGameEventService gameEventService,
        IGamePhaseService gamePhaseService,
        IInputManager inputManager,
        ISpawnSelectionService spawnSelectionService,
        ITableEditorPickerService tableEditorPickerService,
        IMainThread mainThread,
        MdxManager mdxManager,
        AnimationSystem animationSystem,
        Engine engine,
        
        SpawnDragger spawnDragger,
        SpawnContextMenu spawnContextMenu,
        SpawnInfoBoxWindow infoBoxWindow)
    {
        this.databaseProvider = databaseProvider;
        this.spawnsContainer = spawnsContainer;
        this.gameContext = gameContext;
        this.entityManager = entityManager;
        this.renderManager = renderManager;
        this.gameEventService = gameEventService;
        this.gamePhaseService = gamePhaseService;
        this.inputManager = inputManager;
        this.spawnSelectionService = spawnSelectionService;
        this.tableEditorPickerService = tableEditorPickerService;
        this.mainThread = mainThread;
        this.mdxManager = mdxManager;
        this.animationSystem = animationSystem;
        this.engine = engine;
        this.infoBoxWindow = infoBoxWindow;
        this.spawnDragger = spawnDragger;
        this.spawnContextMenu = spawnContextMenu;
        postProcess = new HighlightPostProcess(gameContext.Engine, Color.Aqua);

        disposables.Add(spawnDragger);
        disposables.Add(gameEventService.ActiveEventsObservable.Subscribe(_ => RefreshVisibility()));
        disposables.Add(gamePhaseService.ActivePhasesObservable.Subscribe(_ => RefreshVisibility()));
    }
    
    public void Initialize()
    {
        renderLayer = gameContext.Engine.RenderManager.RegisterRenderLayer("Map Spawns");
        gameContext.Engine.RenderManager.AddPostprocess(postProcess);
    }
    
    public void Dispose()
    {
        gameContext.Engine.RenderManager.UnregisterRenderLayer(renderLayer);
        spawnSelectionService.SelectedSpawn.Value = null;
        spawnsContainer.Clear();
        
        foreach (var spawn in spawnsContainer.Spawns.GetChildren())
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }
        
        foreach (var d in disposables)
            d.Dispose();
        disposables.Clear();
        gameContext.Engine.RenderManager.RemovePostprocess(postProcess);
        postProcess.Dispose();
    }

    public void Update(float delta)
    {
        UpdateSpawnsData();
        
        if (spawnDragger.Update(delta))
            return;
        
        if (inputManager.Mouse.HasJustClicked(MouseButton.Left) ||
            (spawnSelectionService.SelectedSpawn.Value == null && inputManager.Mouse.HasJustClicked(MouseButton.Right)))
        {
            spawnSelectionService.SelectedSpawn.Value = null;
            var pickedEntity = renderManager.PickObject(inputManager.Mouse.NormalizedPosition);

            if (pickedEntity.IsEmpty())
                return;

            pickedEntity = pickedEntity.GetRoot(entityManager);

            if (!entityManager.Exist(pickedEntity))
                return;

            if (!entityManager.HasManagedComponent<SpawnInstance>(pickedEntity))
                return;
            
            var spawn = spawnSelectionService.SelectedSpawn.Value = entityManager.GetManagedComponent<SpawnInstance>(pickedEntity);

            if (inputManager.Mouse.HasJustDoubleClicked)
            {
                // double click, open editor
                mainThread.Dispatch(() =>
                {
                    OpenEditor(spawn).ListenErrors();
                });
            }
        }
    }

    public IEnumerable<(string, ICommand, object?)>? GenerateContextMenu()
    {
        return spawnContextMenu.GenerateContextMenu();
    }

    private async Task OpenEditor(SpawnInstance spawn)
    {
        await tableEditorPickerService.ShowForeignKey1To1(spawn is CreatureSpawnInstance ? DatabaseTable.WorldTable("creature") : DatabaseTable.WorldTable("gameobject"), new(spawn.Guid));
        // here we can reload the spawn to load new values
    }

    public void Render(float delta)
    {
        postProcess.Render([spawnSelectionService.SelectedSpawn.Value?.WorldObject?.WorldObjectEntity]);
    }

    public void RenderGUI()
    {
        infoBoxWindow.RenderGUI();
    }

    public void RenderTransparent()
    {
        spawnDragger.RenderTransparent();
    }

    private int? loadedMap;
    private void UpdateSpawnsData()
    {
        if (loadedMap.HasValue && loadedMap.Value == gameContext.CurrentMap.Id)
            return;

        loadedMap = (int)gameContext.CurrentMap.Id;
        LoadSpawnDataCoroutine(loadedMap.Value).FireAndForget();
    }

    private async ValueTask LoadSpawnDataCoroutine(int mapId)
    {
        while (spawnsContainer.IsLoading && gameContext.CurrentMap.Id == mapId)
            await engine.NextFrame;

        if (gameContext.CurrentMap.Id != mapId) // could have changed while waiting
            return;
        
        spawnsContainer.LoadMap(mapId);
    }
    
    private void RefreshVisibility()
    {
        foreach (var spawn in spawnsContainer.Spawns.GetChildren())
        {
            if (!spawn.IsSpawned)
                continue;
            
            spawn.WorldObject!.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) &&
                                                 spawn.IsVisibleInEvents(gameEventService);
        }
    }

    public async ValueTask LoadChunk(int mapId, int chunkX, int chunkZ, CancellationToken cancellationToken)
    {
        while ((spawnsContainer.IsLoading || spawnsContainer.LoadedMap != mapId)
               && !cancellationToken.IsCancellationRequested)
            await engine.NextFrame;

        if (cancellationToken.IsCancellationRequested)
            return;

        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            if (spawn is CreatureSpawnInstance creatureSpawnInstance)
            {
                var template = databaseProvider.GetCachedCreatureTemplate(spawn.Entry);

                if (template == null)
                    continue;

                var creatureInstance = new CreatureInstance(gameContext, template, null, renderLayer);

                await creatureInstance.Load();

                if (cancellationToken.IsCancellationRequested)
                    return;

                creatureInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) && 
                                                   spawn.IsVisibleInEvents(gameEventService);
                creatureSpawnInstance.Creature = creatureInstance;

                creatureInstance.Position = creatureSpawnInstance.Position;
                creatureInstance.Orientation = creatureSpawnInstance.Orientation;
                creatureInstance.Animation = M2AnimationType.Stand;

                if (creatureSpawnInstance.Equipment is { } eq)
                {
                    await creatureInstance.SetVirtualItem(0, eq.Item1, cancellationToken);
                    await creatureInstance.SetVirtualItem(1, eq.Item2, cancellationToken);
                    await creatureInstance.SetVirtualItem(2, eq.Item3, cancellationToken);
                }

                // the awaits above yield frames; a map change meanwhile disposes the spawn
                // (destroying the creature's entities) and cancels the token
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (creatureSpawnInstance.Addon is {} addon)
                {
                    creatureInstance.Animation = animationSystem.GetAnimationType(creatureInstance.Model,
                        addon.Emote, addon.StandState, (AnimTier)addon.AnimTier) ?? M2AnimationType.Stand;

                    if (addon.Mount != 0)
                    {
                        var mountModel = await mdxManager.LoadCreatureModel(addon.Mount);
                        if (mountModel != null && !cancellationToken.IsCancellationRequested)
                            creatureInstance.Mount = mountModel;
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                    return;

                entityManager.AddManagedComponent(creatureInstance.WorldObjectEntity, spawn);
            }
            else if (spawn is GameObjectSpawnInstance gameObjectSpawnInstance)
            {
                var template = databaseProvider.GetCachedGameObjectTemplate(spawn.Entry);

                if (template == null)
                    continue;

                var gameobjectInstance = new GameObjectInstance(gameContext, template, null, renderLayer);

                await gameobjectInstance.Load();

                if (cancellationToken.IsCancellationRequested)
                    return;

                gameobjectInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) && 
                                                     spawn.IsVisibleInEvents(gameEventService);
                gameObjectSpawnInstance.GameObject = gameobjectInstance;

                gameobjectInstance.Position = gameObjectSpawnInstance.Position;
                gameobjectInstance.Rotation = gameObjectSpawnInstance.Rotation;
                
                entityManager.AddManagedComponent(gameobjectInstance.WorldObjectEntity, spawn);
            }
        }
    }

    public async ValueTask UnloadChunk(int chunkX, int chunkZ)
    {
        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!)
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }
    }
}