using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Events;
using WDE.Common.Events;
using WDE.Common.Solution;
using WDE.Common.Utils;
using WDE.MapSpawns.Models;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Bridge;

/// <summary>
/// Answers the game-side "which scripts does this spawn have" requests on the UI thread by asking
/// every registered <see cref="ISpawnScriptSourceProvider"/> (smart scripts, EventAI, dbscripts —
/// each script editor module contributes its own, so the answer is per-core by construction) and
/// opens the picked slot's editor via <see cref="EventRequestOpenItem"/>.
/// </summary>
[AutoRegister]
[SingleInstance]
public class SpawnScriptsBridge
{
    private readonly IEventAggregator eventAggregator;
    private readonly List<ISpawnScriptSourceProvider> providers;

    private bool activated;

    // all state below is touched only on the UI thread (subscriptions + await continuations)
    private readonly Dictionary<SpawnScriptOwner, IReadOnlyList<SpawnScriptSlot>> cache = new();
    private SpawnScriptOwner? queuedRequest;
    private bool busy;

    public SpawnScriptsBridge(IEventAggregator eventAggregator,
        IEnumerable<ISpawnScriptSourceProvider> providers)
    {
        this.eventAggregator = eventAggregator;
        this.providers = providers.ToList();
    }

    public void Activate()
    {
        if (activated)
            return;
        activated = true;

        eventAggregator.GetEvent<SpawnScriptsRequestedEvent>()
            .Subscribe(o => Run(() => OnRequest(o)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<SpawnScriptOpenRequestedEvent>()
            .Subscribe(r => Run(() => OnOpen(r)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<MovementScriptOpenRequestedEvent>()
            .Subscribe(id => Run(() => OnOpenMovementScript(id)), ThreadOption.UIThread, true);
        eventAggregator.GetEvent<MovementScriptIdSuggestRequestedEvent>()
            .Subscribe(() => Run(OnSuggestMovementScriptId), ThreadOption.UIThread, true);
        // the game client asks us to re-announce when it comes online (startup ordering fix)
        eventAggregator.GetEvent<SpawnScriptsAvailabilityRequestedEvent>()
            .Subscribe(() => eventAggregator.GetEvent<SpawnScriptsAvailableEvent>().Publish(),
                ThreadOption.UIThread, true);

        eventAggregator.GetEvent<SpawnScriptsAvailableEvent>().Publish();
    }

    private void Run(Func<Task> action) => action().ListenErrors();

    /// <summary>Coalescing FIFO worker: while a lookup is awaiting the database, newer requests just
    /// replace the queued one, so the last published state always matches the last requested owner
    /// (the client treats "latest response" as the answer).</summary>
    private async Task OnRequest(SpawnScriptOwner owner)
    {
        queuedRequest = owner;
        if (busy)
            return;
        busy = true;
        try
        {
            while (queuedRequest is { } next)
            {
                queuedRequest = null;
                var slots = await ComputeSlots(next);
                Remember(next, slots);
                eventAggregator.GetEvent<SpawnScriptsChangedEvent>().Publish(new SpawnScriptsState
                {
                    Owner = next,
                    Slots = slots.Select(s => new SpawnScriptSlotInfo(s.Name, s.Detail, s.Exists, s.SolutionItem != null)).ToList()
                });
            }
        }
        finally
        {
            busy = false;
        }
    }

    private async Task OnOpen(SpawnScriptOpenRequest r)
    {
        if (!cache.TryGetValue(r.Owner, out var slots))
            Remember(r.Owner, slots = await ComputeSlots(r.Owner));
        if (r.SlotIndex < 0 || r.SlotIndex >= slots.Count)
            return;
        if (slots[r.SlotIndex].SolutionItem is { } item)
            eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
    }

    private async Task OnOpenMovementScript(uint scriptId)
    {
        foreach (var provider in providers)
        {
            if (await provider.CreateMovementScriptItem(scriptId) is { } item)
            {
                eventAggregator.GetEvent<EventRequestOpenItem>().Publish(item);
                return;
            }
        }
    }

    private async Task OnSuggestMovementScriptId()
    {
        foreach (var provider in providers)
        {
            if (await provider.SuggestFreeMovementScriptId() is { } id)
            {
                eventAggregator.GetEvent<MovementScriptIdSuggestedEvent>().Publish(id);
                return;
            }
        }
    }

    private async Task<IReadOnlyList<SpawnScriptSlot>> ComputeSlots(SpawnScriptOwner owner)
    {
        var slots = new List<SpawnScriptSlot>();
        foreach (var provider in providers)
        {
            try
            {
                slots.AddRange(await provider.GetScripts(owner));
            }
            catch (Exception e)
            {
                Console.WriteLine($"[SpawnScriptsBridge] {provider.GetType().Name} failed: {e}");
            }
        }
        slots.Sort((a, b) => a.Order != b.Order
            ? a.Order.CompareTo(b.Order)
            : string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return slots;
    }

    private void Remember(SpawnScriptOwner owner, IReadOnlyList<SpawnScriptSlot> slots)
    {
        // the cache only backs OnOpen for recently shown spawns - no need to grow forever
        if (cache.Count > 128)
            cache.Clear();
        cache[owner] = slots;
    }
}
