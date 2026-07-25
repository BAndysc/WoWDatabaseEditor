using System.Collections.Concurrent;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

public enum GameNotificationType
{
    Success,
    Error,
    Info,
}

public readonly record struct GameNotification(GameNotificationType Type, string Message);

/// <summary>
/// Toast notifications drawn INSIDE the "3D" view - the app status bar is outside the user's focus
/// while world editing, so save results / errors published only there are effectively invisible.
/// Enqueue is thread-safe (UI-thread bridge events, task continuations, engine thread all publish);
/// <see cref="Rendering.GameViewNotifications"/> drains and draws on the render thread. Also mirrors the
/// bridge's <see cref="WorldSpawnEditNotificationEvent"/> (spawn-save results) as in-view toasts.
/// </summary>
[UniqueProvider]
public interface IGameNotificationService
{
    void Notify(GameNotificationType type, string message);
    bool TryDequeue(out GameNotification notification);
}

public class GameNotificationService : IGameNotificationService
{
    private readonly ConcurrentQueue<GameNotification> queue = new();

    public GameNotificationService(IEventAggregator eventAggregator)
    {
        // keepSubscriberReferenceAlive: this is a singleton that lives for the app's lifetime
        eventAggregator.GetEvent<WorldSpawnEditNotificationEvent>()
            .Subscribe(n => Notify(n.Success ? GameNotificationType.Success : GameNotificationType.Error, n.Message),
                ThreadOption.PublisherThread, keepSubscriberReferenceAlive: true);
    }

    public void Notify(GameNotificationType type, string message) => queue.Enqueue(new GameNotification(type, message));

    public bool TryDequeue(out GameNotification notification) => queue.TryDequeue(out notification);
}

public static class GameNotificationTaskExtensions
{
    /// <summary>Like ListenErrors, but ALSO surfaces the failure to the user as an in-view error
    /// toast: "&lt;context&gt;: &lt;exception message&gt;".</summary>
    public static void ListenErrors(this Task task, IGameNotificationService notifications, string context)
    {
        task.ContinueWith(t =>
        {
            var e = t.Exception!.InnerExceptions.Count == 1 ? t.Exception.InnerExceptions[0] : t.Exception;
            LOG.LogError(e, "{Context}", context);
            notifications.Notify(GameNotificationType.Error, $"{context}: {e.Message}");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}
