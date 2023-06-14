using Avalonia.Input;
using WDE.Common.Services;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.QueryGenerators.Base;
using WDE.QueryGenerators.Models;
using WDE.SqlQueryGenerator;
using IInputManager = TheEngine.Interfaces.IInputManager;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

public class SpawnDeleter : IMapSpawnModule
{
    private readonly IInputManager inputManager;
    private readonly IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryGenerator;
    private readonly IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryGenerator;
    private readonly IPendingGameChangesService pendingGameChangesService;
    private readonly ISpawnSelectionService spawnSelectionService;

    public SpawnDeleter(IInputManager inputManager,
        IQueryGenerator<CreatureSpawnModelEssentials> creatureQueryGenerator,
        IQueryGenerator<GameObjectSpawnModelEssentials> gameObjectQueryGenerator,

        IPendingGameChangesService pendingGameChangesService,
        ISpawnSelectionService spawnSelectionService)
    {
        this.inputManager = inputManager;
        this.creatureQueryGenerator = creatureQueryGenerator;
        this.gameObjectQueryGenerator = gameObjectQueryGenerator;
        this.pendingGameChangesService = pendingGameChangesService;
        this.spawnSelectionService = spawnSelectionService;
    }

    public void Update(float diff)
    {
        if ((inputManager.Keyboard.IsDown(Key.Back) ||
            inputManager.Keyboard.IsDown(Key.Delete)) &&
            spawnSelectionService.SelectedSpawn.Value is { } spawn)
        {
            Delete(spawn);
        }
    }

    public void Delete(SpawnInstance spawn)
    {
        IQuery? query = spawn is CreatureSpawnInstance
            ? creatureQueryGenerator.Delete(new CreatureSpawnModelEssentials() { Guid = spawn.Guid })
            : gameObjectQueryGenerator.Delete(new GameObjectSpawnModelEssentials() { Guid = spawn.Guid });

        if (query == null)
            return;
            
        pendingGameChangesService.AddQuery(spawn is CreatureSpawnInstance ? GuidType.Creature : GuidType.GameObject, spawn.Entry, spawn.Guid, query);
            
        spawn.Dispose();
        spawnSelectionService.SelectedSpawn.Value = null;
    }
}