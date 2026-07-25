using System.Collections.Concurrent;
using Prism.Events;
using WDE.MapRenderer;
using WDE.MapRenderer.Managers;
using WDE.MapSpawns.Models;
using WDE.MapSpawns.ViewModels;

namespace WDE.MapSpawns.Mcp;

/// <summary>
/// The game-side answering half of the MCP game-view tools: a game module (one instance per game
/// session, registered by <see cref="MapSpawnsModule"/>) that subscribes to the global request
/// events, parks the requests in queues and serves them on the game loop, where touching
/// <see cref="IGameContext"/> / the selection service is legal. While no game runs, no instance
/// exists, requests stay unanswered and the tools time out into a clean error.
/// </summary>
public class McpGameViewBridge : IGameModule
{
    private readonly IGameContext gameContext;
    private readonly ISpawnSelectionService spawnSelectionService;
    private readonly IEventAggregator eventAggregator;
    private readonly List<SubscriptionToken> subscriptions = new();

    private readonly ConcurrentQueue<GameViewInfoRequest> infoRequests = new();
    private readonly ConcurrentQueue<CameraFlyToRequest> flyToRequests = new();
    private readonly ConcurrentQueue<SelectedSpawnRequest> selectionRequests = new();

    public object? ViewModel => null;

    public McpGameViewBridge(IGameContext gameContext,
        ISpawnSelectionService spawnSelectionService,
        IEventAggregator eventAggregator)
    {
        this.gameContext = gameContext;
        this.spawnSelectionService = spawnSelectionService;
        this.eventAggregator = eventAggregator;
    }

    public void Initialize()
    {
        // PublisherThread (= the MCP tool's UI thread): only enqueues, the game loop answers
        subscriptions.Add(eventAggregator.GetEvent<GameViewInfoRequestedEvent>()
            .Subscribe(r => infoRequests.Enqueue(r), ThreadOption.PublisherThread, true));
        subscriptions.Add(eventAggregator.GetEvent<CameraFlyToRequestedEvent>()
            .Subscribe(r => flyToRequests.Enqueue(r), ThreadOption.PublisherThread, true));
        subscriptions.Add(eventAggregator.GetEvent<SelectedSpawnRequestedEvent>()
            .Subscribe(r => selectionRequests.Enqueue(r), ThreadOption.PublisherThread, true));
    }

    public void Update(float delta)
    {
        while (infoRequests.TryDequeue(out var info))
        {
            info.Result.TrySetResult(new GameViewInfo
            {
                MapId = gameContext.CurrentMapId,
                CameraPosition = gameContext.CameraManager.Position
            });
        }

        while (flyToRequests.TryDequeue(out var fly))
        {
            var mapChanged = gameContext.CurrentMapId != fly.Map;
            if (mapChanged)
                gameContext.SetMap(fly.Map, fly.Position); // relocates with the fly-here framing once loaded
            else
                gameContext.CameraManager.Relocate(fly.Position, flyHere: fly.Fly);
            fly.Done.TrySetResult(mapChanged);
        }

        while (selectionRequests.TryDequeue(out var selection))
            selection.Result.TrySetResult(ExtractSelection());
    }

    private SelectedSpawnInfo? ExtractSelection()
    {
        var spawn = spawnSelectionService.SelectedSpawn.Value;
        return spawn switch
        {
            CreatureSpawnInstance c => new SelectedSpawnInfo
            {
                IsCreature = true,
                Guid = c.Guid,
                Entry = c.Entry,
                Name = c.CreatureTemplate.Name,
                MapId = gameContext.CurrentMapId,
                Position = c.WorldObject?.Position ?? c.Position,
                Orientation = c.Orientation,
                IsSpawnedInWorld = c.IsSpawned
            },
            GameObjectSpawnInstance g => new SelectedSpawnInfo
            {
                IsCreature = false,
                Guid = g.Guid,
                Entry = g.Entry,
                Name = g.GameObjectTemplate.Name,
                MapId = gameContext.CurrentMapId,
                Position = g.WorldObject?.Position ?? g.Position,
                Orientation = g.Orientation,
                IsSpawnedInWorld = g.IsSpawned
            },
            _ => null
        };
    }

    public void Dispose()
    {
        foreach (var token in subscriptions)
            token.Dispose();
        subscriptions.Clear();
        // anything still parked will never be served by this instance - cancel instead of leaking
        // the callers into their full timeout
        while (infoRequests.TryDequeue(out var info))
            info.Result.TrySetCanceled();
        while (flyToRequests.TryDequeue(out var fly))
            fly.Done.TrySetCanceled();
        while (selectionRequests.TryDequeue(out var selection))
            selection.Result.TrySetCanceled();
    }
}
