using Prism.Events;
using WDE.Common.Services.Mcp;
using WDE.MapSpawns.Models;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Mcp;

/// <summary>
/// Global-container access to the spawn-edit facade for the MCP tools. <see cref="IWorldSpawnEditService"/>
/// is deliberately registered only in the per-game scoped container (see MapSpawnsGameScopeRegistrar),
/// so tools cannot inject it; but its implementation (<see cref="WorldSpawnEditClient"/>) is a thin,
/// stateless-per-map client over the GLOBAL event aggregator (requests answered by the UI-side
/// WorldSpawnEditBridge, state cached from the global state event), so a second, app-lifetime
/// instance held here observes exactly the same state and drives exactly the same bridge.
/// Everything here runs on the UI thread (MCP MainThread tools + the bridge's UIThread publishes).
/// </summary>
[AutoRegister]
[SingleInstance]
public class McpSpawnEditGateway
{
    private readonly Lazy<IEventAggregator> eventAggregator;
    private IWorldSpawnEditService? service;
    private SpawnEditNotification? lastNotification;
    private long notificationSeq;

    public McpSpawnEditGateway(Lazy<IEventAggregator> eventAggregator)
    {
        this.eventAggregator = eventAggregator;
    }

    public IWorldSpawnEditService Service
    {
        get
        {
            if (service == null)
            {
                service = new WorldSpawnEditClient(eventAggregator.Value);
                // the bridge posts save/create results here (status bar + 3D toasts); cache the last
                // one so spawn_save can report success/failure instead of fire-and-forget silence
                eventAggregator.Value.GetEvent<WorldSpawnEditNotificationEvent>()
                    .Subscribe(n => lastNotification = new SpawnEditNotification(++notificationSeq, n.Success, n.Message),
                        ThreadOption.PublisherThread, keepSubscriberReferenceAlive: true);
            }
            return service;
        }
    }

    /// <summary>Last save/spawn notification published by the edit bridge (monotonic Seq).</summary>
    public SpawnEditNotification? LastNotification => lastNotification;

    /// <summary>Waits (a short grace period) for the edit bridge to report itself available. Needed
    /// because the bridge's UIThread event subscriptions are POSTed by Prism even when published from
    /// the UI thread, so right after the lazily-built client asked for the initial state, the answer
    /// only lands once we yield to the dispatcher.</summary>
    public async Task<bool> WaitForAvailability(int timeoutMs, CancellationToken token)
    {
        var s = Service;
        var deadline = Environment.TickCount64 + timeoutMs;
        while (!s.IsAvailable && Environment.TickCount64 < deadline)
            await Task.Delay(50, token);
        return s.IsAvailable;
    }

    /// <summary>The facade, or a clean MCP error when the 3D view / edit bridge is not running.</summary>
    public async Task<IWorldSpawnEditService> RequireAvailable(CancellationToken token)
    {
        if (!await WaitForAvailability(1000, token))
            throw new McpToolException("The 3D spawn editor is not running. Open the 3D game view first " +
                                       "(load a map in the editor's 3D view) - spawn editing tools only work while it is open.");
        return Service;
    }
}

public sealed record SpawnEditNotification(long Seq, bool Success, string Message);

internal static class McpAwait
{
    /// <summary>Awaits <paramref name="task"/> up to <paramref name="timeoutMs"/>. Returns
    /// (false, default) on timeout or when the request was cancelled (game view closed mid-flight).</summary>
    public static async Task<(bool Completed, T? Result)> WaitFor<T>(Task<T> task, int timeoutMs, CancellationToken token)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeoutMs, token));
        token.ThrowIfCancellationRequested();
        if (winner != task || task.IsCanceled)
            return (false, default);
        return (true, await task);
    }

    /// <summary>Polls until the edit bridge published a new state (Revision changed) - the mutation
    /// events are fire-and-forget and handled asynchronously on the UI thread, so a fresh snapshot
    /// only exists once the bridge got around to it. False = not confirmed within the timeout.</summary>
    public static async Task<bool> WaitForRevisionChange(IWorldSpawnEditService service, int previousRevision,
        int timeoutMs, CancellationToken token)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (service.Revision == previousRevision && Environment.TickCount64 < deadline)
            await Task.Delay(50, token);
        return service.Revision != previousRevision;
    }
}
