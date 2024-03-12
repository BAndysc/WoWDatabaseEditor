using System.Collections;
using Avalonia.Input;
using TheEngine.PhysicsSystem;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Tasks;
using WDE.Common.Utils;
using WDE.MapRenderer.Managers;
using WDE.MapRenderer.Managers.Entities;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.MpqReader.Structures;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using IInputManager = TheEngine.Interfaces.IInputManager;
using MouseButton = TheEngine.Input.MouseButton;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

public class NewSpawnCreator : System.IDisposable, IMapSpawnModule
{
    private readonly IGameContext gameContext;
    private readonly RaycastSystem raycastSystem;
    private readonly ICachedDatabaseProvider databaseProvider;
    private readonly IInputManager inputManager;
    private readonly ISpawnsContainer spawnsContainer;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IPersonalGuidRangeService personalGuidRangeService;
    private readonly IMessageBoxService messageBoxService;
    private readonly IMainThread mainThread;
    private readonly IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryGenerator;
    private readonly IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryGenerator;
    private readonly IPendingGameChangesService pendingGameChangesService;
    private readonly ZoneAreaManager zoneAreaManager;
    private readonly DbcManager dbcManager;
    private readonly AnimationSystem animationSystem;
    private readonly GamePhaseService gamePhaseService;
    private readonly SpawnDragger spawnDragger;
    private readonly QuickSpawnMenu quickSpawnMenu;

    public NewSpawnCreator(IGameContext gameContext,
        RaycastSystem raycastSystem,
        ICachedDatabaseProvider databaseProvider,
        IInputManager inputManager,
        ISpawnsContainer spawnsContainer,
        ISpawnSelectionService spawnSelectionService,
        IPersonalGuidRangeService personalGuidRangeService,
        IMessageBoxService messageBoxService,
        IMainThread mainThread,
        
        IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryGenerator,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryGenerator,
        IPendingGameChangesService pendingGameChangesService,
        
        ZoneAreaManager zoneAreaManager,
        DbcManager dbcManager,
        AnimationSystem animationSystem,
        
        GamePhaseService gamePhaseService,
        SpawnDragger spawnDragger,
        QuickSpawnMenu quickSpawnMenu)
    {
        this.gameContext = gameContext;
        this.raycastSystem = raycastSystem;
        this.databaseProvider = databaseProvider;
        this.inputManager = inputManager;
        this.spawnsContainer = spawnsContainer;
        this.spawnSelectionService = spawnSelectionService;
        this.personalGuidRangeService = personalGuidRangeService;
        this.messageBoxService = messageBoxService;
        this.mainThread = mainThread;
        this.creatureQueryGenerator = creatureQueryGenerator;
        this.gameObjectQueryGenerator = gameObjectQueryGenerator;
        this.pendingGameChangesService = pendingGameChangesService;
        this.zoneAreaManager = zoneAreaManager;
        this.dbcManager = dbcManager;
        this.animationSystem = animationSystem;
        this.gamePhaseService = gamePhaseService;
        this.spawnDragger = spawnDragger;
        this.quickSpawnMenu = quickSpawnMenu;
    }
    
    public void SpawnAndDrag(bool creature, uint entry)
    {
        gameContext.StartCoroutine(SpawnAndDragCoroutine(creature, entry));
    }

    public void Dispose()
    {
        quickSpawnMenu.Dispose();
    }

    public void Render(float delta)
    {
        quickSpawnMenu.Render(delta);
    }

    public void RenderGUI()
    {
        if (quickSpawnMenu.RenderGui(out var spawnEntry, out var spawnCreature))
            SpawnAndDrag(spawnCreature, spawnEntry);
    }

    private IEnumerator GetNextGuid(GuidType type, TaskCompletionSource<uint?> task)
    {
        mainThread.Dispatch(() =>
        {
            async Task Do()
            {
                var result = await personalGuidRangeService.GetNextGuidOrShowError(type, messageBoxService);
                task.SetResult(result);
            }

            Do().ListenErrors();
        });
        yield return task.Task;
    }

    private IEnumerator SpawnAndDragCoroutine(bool creature, uint entry)
    {
        yield return null;
        WorldObjectInstance? worldObjectInstance;
        SpawnInstance dummySpawn;
        var hit = raycastSystem.RaycastMouse(Collisions.COLLISION_MASK_STATIC);

        var position = hit?.Item2 ?? Vector3.Zero;
        
        if (creature)
        {
            var template = databaseProvider.GetCachedCreatureTemplate(entry);
            if (template == null)
                yield break;

            var templateAddon = databaseProvider.GetCreatureTemplateAddon(entry);
            yield return templateAddon;
            
            CreatureInstance ci = new CreatureInstance(gameContext, template, null);
            yield return ci.Load();
            if (templateAddon.Result is {} addon)
            {
                ci.Animation = animationSystem.GetAnimationType(ci.Model, addon.Emote, addon.StandState, (AnimTier)addon.AnimTier) ?? M2AnimationType.Stand;

                if (addon.Mount != 0)
                    yield return ci.LoadMount(addon.Mount);   
            }

            ci.MaterialRenderData.SetInt(ci.BaseMaterial, "translucent", 1);
            worldObjectInstance = ci;
            var dummySpawnInstance = new CreatureSpawnInstance(new DummyCreature(){X = position.X, Y = position.Y, Z = position.Z}, template, null);
            dummySpawnInstance.Creature = ci;
            dummySpawn = dummySpawnInstance;
        }
        else
        {
            var template = databaseProvider.GetCachedGameObjectTemplate(entry);
            if (template == null)
                yield break;
            
            GameObjectInstance gi = new GameObjectInstance(gameContext, template, null);
            yield return gi.Load();
            gi.MaterialRenderData.SetInt(gi.BaseMaterial, "translucent", 1);
            worldObjectInstance = gi;
            var dummySpawnInstance = new GameObjectSpawnInstance(new DummyGameObject(){X = position.X, Y = position.Y, Z = position.Z}, template, null);
            dummySpawnInstance.GameObject = gi;
            dummySpawn = dummySpawnInstance;
        }

        worldObjectInstance.Position = position;

        spawnSelectionService.SelectedSpawn.Value = dummySpawn;
        yield return null;
        spawnDragger.BeginDrag();

        bool accept = false;
        while (true)
        {
            if (inputManager.Mouse.HasJustClicked(MouseButton.Left))
            {
                accept = true;
                break;
            }
            
            if (inputManager.Keyboard.JustPressed(Key.Escape))
                break;
                
            yield return null;
        }

        spawnDragger.FinishDrag();
        spawnSelectionService.SelectedSpawn.Value = null;
        if (accept)
        {
            TaskCompletionSource<uint?> guidTask = new();
            yield return GetNextGuid(creature ? GuidType.Creature : GuidType.GameObject, guidTask);
            if (!guidTask.Task.Result.HasValue)
            {
                worldObjectInstance.Dispose();
                yield break;
            }
            var guid = guidTask.Task.Result.Value;

            if (creature)
            {
                var query = creatureQueryGenerator.Insert(new CreatureSpawnModelEssentials()
                {
                    Guid = guid,
                    Entry = entry,
                    Map = gameContext.CurrentMap.Id,
                    SpawnMask = 1,
                    PhaseMask = 1,
                    PhaseId = new SmallReadOnlyList<uint>(gamePhaseService.Phases.Where(p => p.Active && p.IsPhaseId).Select(x => x.Entry)),
                    X = worldObjectInstance.Position.X,
                    Y = worldObjectInstance.Position.Y,
                    Z = worldObjectInstance.Position.Z,
                    O = ((CreatureInstance)worldObjectInstance).Orientation
                });
                pendingGameChangesService.AddQuery(GuidType.Creature, entry, guid, query!);
                yield return pendingGameChangesService.SaveAll();
            }
            else
            {
                var rot = ((GameObjectInstance)worldObjectInstance).Rotation;
                var query = gameObjectQueryGenerator.Insert(new GameObjectSpawnModelEssentials()
                {
                    Guid = guid,
                    Entry = entry,
                    Map = gameContext.CurrentMap.Id,
                    SpawnMask = 1,
                    PhaseMask = 1,
                    PhaseId = new SmallReadOnlyList<uint>(gamePhaseService.Phases.Where(p => p.Active && p.IsPhaseId).Select(x => x.Entry)),
                    X = worldObjectInstance.Position.X,
                    Y = worldObjectInstance.Position.Y,
                    Z = worldObjectInstance.Position.Z,
                    Rotation0 = rot.X,
                    Rotation1 = rot.Y,
                    Rotation2 = rot.Z,
                    Rotation3 = rot.W,
                    State = 1
                });
                pendingGameChangesService.AddQuery(GuidType.GameObject, entry, guid, query!);
                yield return pendingGameChangesService.SaveAll();
            }
            
            if (creature)
                yield return spawnsContainer.ReloadCreature(entry, guid, zoneAreaManager, dbcManager);
            else
                yield return spawnsContainer.ReloadGameobject(entry, guid, zoneAreaManager, dbcManager);
        }
        worldObjectInstance.Dispose();
    }
}