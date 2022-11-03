using System.Collections;
using TheEngine.Interfaces;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.MapRenderer.Managers;
using WDE.MapSpawnsEditor.Models;
using WDE.MapSpawnsEditor.ViewModels;
using WDE.Module.Attributes;

namespace WDE.MapSpawnsEditor.Rendering.Modules;

[AutoRegister]
public class MovementVisualizer : IMapSpawnModule
{
    private readonly IDatabaseProvider databaseProvider;
    private readonly IRenderManager renderManager;
    private readonly IGameContext gameContext;
    private readonly ICurrentCoreVersion coreVersion;
    private readonly ISpawnSelectionService selectionService;

    private IReadOnlyList<IWaypoint>? currentWaypoints;
    private uint currentCreatureGuid;
    private Vector3 startPosition;
    private float randomDist;
    private MovementType movementType;
    
    public MovementVisualizer(IDatabaseProvider databaseProvider,
        IRenderManager renderManager,
        IGameContext gameContext,
        ICurrentCoreVersion coreVersion,
        
        ISpawnSelectionService selectionService)
    {
        this.databaseProvider = databaseProvider;
        this.renderManager = renderManager;
        this.gameContext = gameContext;
        this.coreVersion = coreVersion;
        this.selectionService = selectionService;
    }

    public void Update(float diff)
    {
        var selected = selectionService.SelectedSpawn.Value;
        if (selected == null ||
            selected is GameObjectSpawnInstance)
        {
            currentWaypoints = null;
            currentCreatureGuid = 0;
        }
        else if (selected is CreatureSpawnInstance creature)
        {
            if (selected.Guid != currentCreatureGuid)
            {
                currentWaypoints = null;
                currentCreatureGuid = selected.Guid;
                startPosition = creature.Creature?.Position ?? creature.Position;
                randomDist = creature.WanderDistance;
                movementType = creature.MovementType;
                if (movementType == MovementType.Waypoint || creature.SpawnGroup != null)
                    gameContext.StartCoroutine(LoadWaypointsForSpawn(creature));
            }   
        }
    }

    private IEnumerator LoadWaypointsForSpawn(CreatureSpawnInstance selected)
    {
        if (selected.SpawnGroup != null)
        {
            var spawnGroupTask = databaseProvider.GetSpawnGroupFormation(selected.SpawnGroup.Id);
            yield return spawnGroupTask;

            var formation = spawnGroupTask.Result;
            if (formation != null && formation.PathId != 0)
            {
                var task = databaseProvider.GetMangosWaypoints((uint)formation.PathId);
                yield return task;

                if (currentCreatureGuid == selected.Guid)
                {
                    currentWaypoints = task.Result;
                    movementType = formation.MovementType;
                }
                yield break;
            }
        }

        if (selected.Addon != null && selected.Addon.PathId != 0)
        {
            var task = databaseProvider.GetWaypointData(selected.Addon!.PathId);
            yield return task;
            if (currentCreatureGuid == selected.Guid)
                currentWaypoints = task.Result;   
        }
        
        if (coreVersion.Current.DatabaseFeatures.SupportedWaypoints.HasFlagFast(WaypointTables.MangosCreatureMovement))
        {
            var task = databaseProvider.GetMangosCreatureMovement(selected.Guid);
            yield return task;
            if (currentCreatureGuid == selected.Guid)
                currentWaypoints = task.Result;
        }
    }

    public void RenderTransparent(float diff)
    {
        if (movementType == MovementType.Waypoint && currentWaypoints != null)
        {
            var previousPoint = startPosition;
            for (int i = 0; i < currentWaypoints.Count; ++i)
            {
                var p = new Vector3(currentWaypoints[i].X, currentWaypoints[i].Y, currentWaypoints[i].Z);
                renderManager.DrawLine(previousPoint, p, Vector4.One);
                previousPoint = p;
            }
        }
        else if (movementType == MovementType.Random && randomDist > 0)
        {
            renderManager.DrawSphere(startPosition, randomDist, Vector4.One);
        }
    }
}