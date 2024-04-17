using System.Collections;
using System.Windows.Input;
using TheEngine.Components;
using TheEngine.ECS;
using TheEngine.Interfaces;
using TheEngine.PhysicsSystem;
using TheMaths;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapRenderer.Utils;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.Rendering.Modules;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;
using WDE.MpqReader.DBC;
using WDE.MpqReader.Structures;
using WDE.MVVM.Observable;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawnsEditor.Rendering;

[UniqueProvider]
public interface ISpawnViewerProxy
{
    SpawnViewer? CurrentViewer { get; set; }
}

[AutoRegister]
[SingleInstance]
public class SpawnViewerProxy : ISpawnViewerProxy
{
    public SpawnViewer? CurrentViewer { get; set; }
}

public class SpawnViewer : IGameModule, ISavable
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
    private readonly ISpawnViewerProxy spawnViewerProxy;
    private readonly IPendingGameChangesService pendingGameChangesService;
    private readonly IMessageBoxService messageBoxService;
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly IChangesManager changesManager;
    private readonly RaycastSystem raycastSystem;
    private readonly MdxManager mdxManager;
    private readonly AnimationSystem animationSystem;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly DbcManager dbcManager;
    public object? ViewModel => null;

    private HighlightPostProcess postProcess;
    
    private List<System.IDisposable> disposables = new();

    private SpawnDragger spawnDragger;
    private readonly ISpawnContextMenu spawnContextMenu;
    private readonly NewSpawnCreator newSpawnCreator;
    private readonly List<IMapSpawnModule> mapModules;

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
        ISpawnViewerProxy spawnViewerProxy,
        IPendingGameChangesService pendingGameChangesService,
        IMessageBoxService messageBoxService,
        IMySqlExecutor mySqlExecutor,
        IChangesManager changesManager,
        RaycastSystem raycastSystem,
        MdxManager mdxManager,
        AnimationSystem animationSystem,
        ZoneAreaManager zoneAreaManager,
        DbcManager dbcManager,
        
        PathEditor pathEditor,
        SpawnDragger spawnDragger,
        ISpawnContextMenu spawnContextMenu,
        NewSpawnCreator newSpawnCreator,
        IEnumerable<IMapSpawnModule> mapModules)
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
        this.spawnViewerProxy = spawnViewerProxy;
        this.pendingGameChangesService = pendingGameChangesService;
        this.messageBoxService = messageBoxService;
        this.mySqlExecutor = mySqlExecutor;
        this.changesManager = changesManager;
        this.raycastSystem = raycastSystem;
        this.mdxManager = mdxManager;
        this.animationSystem = animationSystem;
        this.zoneAreaManager = zoneAreaManager;
        this.dbcManager = dbcManager;
        this.spawnDragger = spawnDragger;
        this.spawnContextMenu = spawnContextMenu;
        this.newSpawnCreator = newSpawnCreator;
        this.mapModules = mapModules.ToList();
        this.mapModules.Add(pathEditor);
        postProcess = new HighlightPostProcess(gameContext.Engine, Color.Aqua);

        spawnViewerProxy.CurrentViewer = this;
        
        disposables.Add(spawnDragger);
        disposables.Add(changesManager.AddSavable(this));
        disposables.Add(gameEventService.ActiveEventsObservable.Subscribe(_ => RefreshVisibility()));
        disposables.Add(gamePhaseService.ActivePhasesObservable.Subscribe(_ => RefreshVisibility()));
        pendingGameChangesService.HasPendingChanged += PendingGameChangesServiceOnHasPendingChanged;
    }

    public void Initialize()
    {
        spawnsContainer.OnCreatureModified += SpawnsContainerOnOnCreatureModified;
        spawnsContainer.OnGameobjectModified += SpawnsContainerOnOnGameobjectModified;
        pendingGameChangesService.RequestReload += RequestReloadGuids;
        gameContext.Engine.RenderManager.AddPostprocess(postProcess);
    }

    public void Dispose()
    {
        pendingGameChangesService.RequestReload -= RequestReloadGuids;
        foreach (var spawn in spawnsContainer.Spawns.GetChildren())
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }

        foreach (var module in mapModules)
        {
            if (module is IDisposable disp)
                disp.Dispose();
        }
        newSpawnCreator.Dispose();

        pendingGameChangesService.HasPendingChanged -= PendingGameChangesServiceOnHasPendingChanged;
        spawnsContainer.OnCreatureModified -= SpawnsContainerOnOnCreatureModified;
        spawnsContainer.OnGameobjectModified -= SpawnsContainerOnOnGameobjectModified;
        spawnViewerProxy.CurrentViewer = null;
        spawnSelectionService.SelectedSpawn.Value = null;
        spawnsContainer.Clear();
        
        foreach (var d in disposables)
            d.Dispose();
        disposables.Clear();
        gameContext.Engine.RenderManager.RemovePostprocess(postProcess);
        postProcess.Dispose();
    }

    private void PendingGameChangesServiceOnHasPendingChanged()
    {
        IsModified.Value = pendingGameChangesService.HasAnyPendingChanges();
    }

    public void Update(float delta)
    {
        UpdateSpawnsData();
     
        foreach (var module in mapModules)
            module.Update(delta);
        
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
        var guidType = spawn is CreatureSpawnInstance ? GuidType.Creature : GuidType.GameObject;
        var tableName = guidType is GuidType.Creature ? DatabaseTable.WorldTable("creature") : DatabaseTable.WorldTable("gameobject");
        List<(GuidType, uint entry, uint guid)>? toReload = null;
        if (pendingGameChangesService.HasGuidPendingChange(guidType, spawn.Entry, spawn.Guid))
        {
            var result = await messageBoxService.ShowDialog(new MessageBoxFactory<int>()
                .SetTitle("Pending changes")
                .SetMainInstruction("You have pending changes")
                .SetContent($"The {tableName} that you want to edit, was moved/rotated. Before you edit it, you have to either save your changes or revert them. What do you want to do?")
                .WithButton("Save all and edit", 0, true)
                .WithButton("Revert and edit", 1)
                .WithButton("Cancel editing", 2, false, true)
                .Build());
            
            if (result == 2)
                return;

            if (result == 1)
                toReload = pendingGameChangesService.CancelAll();

            if (result == 0)
            {
                await Save();
            }
        }
        await tableEditorPickerService.ShowForeignKey1To1(tableName, new(spawn.Guid));
        await spawnsContainer.Reload(guidType, spawn.Entry, spawn.Guid, zoneAreaManager, dbcManager);
        
        if (toReload != null)
        {
            foreach (var (type, entry, guid) in toReload)
                await spawnsContainer.Reload(type, entry, guid, zoneAreaManager, dbcManager);
        }
    }

    public void Render(float delta)
    {
        postProcess.Render(spawnSelectionService.SelectedSpawn.Value?.WorldObject?.Renderers);
        newSpawnCreator.Render(delta);
        foreach (var module in mapModules)
            module.Render(delta);
    }

    public void RenderGUI()
    {
        foreach (var module in mapModules)
            module.RenderGUI();
        newSpawnCreator.RenderGUI();
    }

    public void RenderTransparent()
    {
        spawnDragger.RenderTransparent();
        foreach (var module in mapModules)
            module.RenderTransparent(0); // todo: pass delta here
    }

    private int? loadedMap;
    private void UpdateSpawnsData()
    {
        if (loadedMap.HasValue && loadedMap.Value == gameContext.CurrentMap.Id)
            return;

        loadedMap = (int)gameContext.CurrentMap.Id;
        gameContext.StartCoroutine(LoadSpawnDataCoroutine(loadedMap.Value));
    }

    private IEnumerator LoadSpawnDataCoroutine(int mapId)
    {
        while (spawnsContainer.IsLoading && gameContext.CurrentMap.Id == mapId)
            yield return null;

        if (gameContext.CurrentMap.Id != mapId) // could have changed while waiting
            yield break;
        
        spawnsContainer.LoadMap(mapId, zoneAreaManager, dbcManager);
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

    public IEnumerator LoadChunk(int mapId, int chunkX, int chunkZ, CancellationToken cancellationToken)
    {
        while ((spawnsContainer.IsLoading || spawnsContainer.LoadedMap != mapId) 
               && !cancellationToken.IsCancellationRequested)
            yield return null;

        if (cancellationToken.IsCancellationRequested)
            yield break;

        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            if (spawn is CreatureSpawnInstance creatureSpawnInstance)
            {
                yield return ReloadCreature(creatureSpawnInstance);
            }
            else if (spawn is GameObjectSpawnInstance gameObjectSpawnInstance)
            {
                yield return ReloadGameobject(gameObjectSpawnInstance);
            }
        }
    }

    public IEnumerator UnloadChunk(int chunkX, int chunkZ)
    {
        foreach (var spawn in spawnsContainer.SpawnsPerChunk[chunkX, chunkZ]!)
        {
            if (spawn.IsSpawned)
                spawn.Dispose();
        }

        yield break;
    }
    
    private void SpawnsContainerOnOnGameobjectModified(GameObjectSpawnInstance obj)
    {
        gameContext.StartCoroutine(ReloadGameobject(obj));
    }

    private void SpawnsContainerOnOnCreatureModified(CreatureSpawnInstance obj)
    {
        gameContext.StartCoroutine(ReloadCreature(obj));
    }

    private void RequestReloadGuids(List<(GuidType, uint entry, uint guid)> toReload)
    {
        async Task Do()
        {
            foreach (var (type, entry, guid) in toReload)
                await spawnsContainer.Reload(type, entry, guid, zoneAreaManager, dbcManager);
        }
        Do().ListenErrors();
    }
    
    private IEnumerator ReloadCreature(CreatureSpawnInstance spawn)
    {
        yield return null;
        if (spawn.Creature != null)
        {
            spawn.Dispose();
        }
        
        var template = databaseProvider.GetCachedCreatureTemplate(spawn.Entry);

        if (template == null)
            yield break;

        var creatureInstance = new CreatureInstance(gameContext, template, spawn.CreatureDisplayId == 0 ? null : spawn.CreatureDisplayId);

        yield return creatureInstance.Load();

        creatureInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) && 
                                           spawn.IsVisibleInEvents(gameEventService);
        spawn.Creature = creatureInstance;

        creatureInstance.Position = spawn.Position;
        creatureInstance.Orientation = spawn.Orientation;
        creatureInstance.Animation = M2AnimationType.Stand;

        if (spawn.Equipment is { } eq)
        {
            yield return creatureInstance.SetVirtualItem(0, eq.Item1);
            yield return creatureInstance.SetVirtualItem(1, eq.Item2);
            yield return creatureInstance.SetVirtualItem(2, eq.Item3);
        }
        
        if (spawn.Addon is {} addon)
        {
            creatureInstance.Animation = animationSystem.GetAnimationType(creatureInstance.Model, addon.Emote, addon.StandState, (AnimTier)addon.AnimTier) ?? M2AnimationType.Stand;

            if (addon.Mount != 0)
                yield return creatureInstance.LoadMount(addon.Mount);
        }
        
        entityManager.AddManagedComponent<SpawnInstance>(creatureInstance.WorldObjectEntity, spawn);
    }

    private IEnumerator ReloadGameobject(GameObjectSpawnInstance spawn)
    {
        yield return null;
        if (spawn.GameObject != null)
        {
            spawn.Dispose();
        }
        
        var template = databaseProvider.GetCachedGameObjectTemplate(spawn.Entry);

        if (template == null)
            yield break;

        var gameobjectInstance = new GameObjectInstance(gameContext, template, null);

        yield return gameobjectInstance.Load();

        gameobjectInstance.EnableRendering = spawn.IsVisibleInPhase(gamePhaseService) && 
                                             spawn.IsVisibleInEvents(gameEventService);
        spawn.GameObject = gameobjectInstance;

        gameobjectInstance.Position = spawn.Position;
        gameobjectInstance.Rotation = spawn.Rotation;
                
        entityManager.AddManagedComponent<SpawnInstance>(gameobjectInstance.WorldObjectEntity, spawn);
    }

    public void SpawnAndDrag(bool creature, uint entry)
    {
        newSpawnCreator.SpawnAndDrag(creature, entry);
    }

    public async Task Save()
    {
        await pendingGameChangesService.SaveAll();
    }

    public ReactiveProperty<bool> IsModified { get; } = new ReactiveProperty<bool>(false);
}

public class DummyCreature : ICreature
{
    public uint Guid { get; set; }
    public uint Entry { get; set; }
    public int Map { get; set; }
    public uint? PhaseMask { get; set; }
    public SmallReadOnlyList<int>? PhaseId { get; set; }
    public int? PhaseGroup { get; set; }
    public int EquipmentId { get; set; }
    public uint Model { get; set; }
    public MovementType MovementType { get; set; }
    public float WanderDistance { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float O { get; set; }
}

public class DummyGameObject : IGameObject
{
    public uint Guid { get; set; }
    public uint Entry { get; set; }
    public int Map { get; set; }
    public uint? PhaseMask { get; set; }
    public SmallReadOnlyList<int>? PhaseId { get; set; }
    public int? PhaseGroup { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Orientation { get; set; }
    public float Rotation0 { get; set; }
    public float Rotation1 { get; set; }
    public float Rotation2 { get; set; }
    public float Rotation3 { get; set; }
    public float ParentRotation0 { get; set; }
    public float ParentRotation1 { get; set; }
    public float ParentRotation2 { get; set; }
    public float ParentRotation3 { get; set; }
}