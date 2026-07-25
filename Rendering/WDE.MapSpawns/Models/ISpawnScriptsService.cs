using Prism.Events;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

/// <summary>
/// The game-side surface for the selected spawn's attached scripts (smart script, EventAI,
/// dbscripts — whatever the current core supports). Same bus pattern as
/// <see cref="IWorldSpawnEditService"/>: <see cref="GetState"/> publishes a request event and
/// returns the cached answer once the bridge (full app only) responds; in headless hosts nobody
/// answers, <see cref="IsAvailable"/> stays false and the UI hides the section.
/// </summary>
[UniqueProvider]
public interface ISpawnScriptsService
{
    /// <summary>True once any bridge response arrived, i.e. the scripts bridge exists.</summary>
    bool IsAvailable { get; }

    /// <summary>Cached slots for the owner, or null while (re)loading. Safe to call every frame;
    /// a request is published only when the owner changes.</summary>
    SpawnScriptsState? GetState(SpawnScriptOwner owner);

    /// <summary>Drop the cache and re-query (e.g. after an editor may have added rows).</summary>
    void Refresh(SpawnScriptOwner owner);

    /// <summary>Open the editor of slot <paramref name="slotIndex"/> of the reported state.</summary>
    void Open(SpawnScriptOwner owner, int slotIndex);

    /// <summary>Open the movement script (cmangos dbscripts_on_creature_movement) a waypoint row's
    /// script id points to (works for a not-yet-existing script too — the editor opens empty).</summary>
    void OpenMovementScript(uint scriptId);

    /// <summary>Ask the bridge for a not-yet-used movement script id; the answer arrives
    /// asynchronously via <see cref="TakeMovementScriptIdSuggestion"/>.</summary>
    void RequestMovementScriptIdSuggestion();

    /// <summary>Returns the pending suggested movement script id (and clears it), or null.</summary>
    uint? TakeMovementScriptIdSuggestion();
}

public class SpawnScriptsClient : ISpawnScriptsService
{
    private readonly IEventAggregator eventAggregator;
    private volatile SpawnScriptsState? state;
    private volatile bool isAvailable;
    private SpawnScriptOwner lastRequested;
    private long suggestedMovementScriptId = -1; // -1 = none pending (Interlocked, arrives off-thread)

    public SpawnScriptsClient(IEventAggregator eventAggregator)
    {
        this.eventAggregator = eventAggregator;
        // keepSubscriberReferenceAlive: this is a singleton that lives for the app's lifetime
        eventAggregator.GetEvent<SpawnScriptsChangedEvent>()
            .Subscribe(s =>
            {
                state = s;
                isAvailable = true;
            }, ThreadOption.PublisherThread, keepSubscriberReferenceAlive: true);
        eventAggregator.GetEvent<SpawnScriptsAvailableEvent>()
            .Subscribe(() => isAvailable = true, ThreadOption.PublisherThread, true);
        eventAggregator.GetEvent<MovementScriptIdSuggestedEvent>()
            .Subscribe(id => System.Threading.Interlocked.Exchange(ref suggestedMovementScriptId, id),
                ThreadOption.PublisherThread, true);
        // the bridge usually activates before this client is built (it activates with the app), so
        // its "available" announce was already emitted - ask it to re-announce now that we listen.
        eventAggregator.GetEvent<SpawnScriptsAvailabilityRequestedEvent>().Publish();
    }

    public bool IsAvailable => isAvailable;

    public SpawnScriptsState? GetState(SpawnScriptOwner owner)
    {
        var s = state;
        if (s != null && s.Owner == owner)
            return s;
        if (lastRequested != owner)
        {
            lastRequested = owner;
            eventAggregator.GetEvent<SpawnScriptsRequestedEvent>().Publish(owner);
        }
        return null;
    }

    public void Refresh(SpawnScriptOwner owner)
    {
        if (state is { } s && s.Owner == owner)
            state = null;
        lastRequested = owner;
        eventAggregator.GetEvent<SpawnScriptsRequestedEvent>().Publish(owner);
    }

    public void Open(SpawnScriptOwner owner, int slotIndex) =>
        eventAggregator.GetEvent<SpawnScriptOpenRequestedEvent>().Publish(new SpawnScriptOpenRequest(owner, slotIndex));

    public void OpenMovementScript(uint scriptId) =>
        eventAggregator.GetEvent<MovementScriptOpenRequestedEvent>().Publish(scriptId);

    public void RequestMovementScriptIdSuggestion() =>
        eventAggregator.GetEvent<MovementScriptIdSuggestRequestedEvent>().Publish();

    public uint? TakeMovementScriptIdSuggestion()
    {
        var value = System.Threading.Interlocked.Exchange(ref suggestedMovementScriptId, -1);
        return value < 0 ? null : (uint)value;
    }
}
