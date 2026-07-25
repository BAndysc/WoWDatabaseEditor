using System.Collections.Generic;
using Prism.Events;
using WDE.Common.Solution;

namespace WDE.MapSpawns.Models;

// --- requests: published by the game side, answered by the scripts bridge (full app only) ---

/// <summary>Ask which script slots (smart script / EventAI / dbscripts...) the spawn has.</summary>
public class SpawnScriptsRequestedEvent : PubSubEvent<SpawnScriptOwner> { }

/// <summary>Open the editor of one previously reported script slot.</summary>
public class SpawnScriptOpenRequestedEvent : PubSubEvent<SpawnScriptOpenRequest> { }

/// <summary>Open the movement script (cmangos dbscripts_on_creature_movement) with the given id,
/// referenced by a waypoint row's script id column.</summary>
public class MovementScriptOpenRequestedEvent : PubSubEvent<uint> { }

/// <summary>Ask for a free movement script id (for attaching a new script to a waypoint).</summary>
public class MovementScriptIdSuggestRequestedEvent : PubSubEvent { }

/// <summary>Published by the game-side client when it comes online, asking the bridge (if any) to
/// announce itself — same startup-ordering fix as <see cref="WorldSpawnEditStateRequestedEvent"/>.</summary>
public class SpawnScriptsAvailabilityRequestedEvent : PubSubEvent { }

// --- state: published back by the bridge, cached by the game for rendering ---

public class SpawnScriptsChangedEvent : PubSubEvent<SpawnScriptsState> { }

/// <summary>The bridge exists (published on activation and on demand).</summary>
public class SpawnScriptsAvailableEvent : PubSubEvent { }

/// <summary>Answer to <see cref="MovementScriptIdSuggestRequestedEvent"/>.</summary>
public class MovementScriptIdSuggestedEvent : PubSubEvent<uint> { }

public readonly record struct SpawnScriptOpenRequest(SpawnScriptOwner Owner, int SlotIndex);

/// <summary>Renderable info about one script slot; index in the list is the open handle.</summary>
public readonly record struct SpawnScriptSlotInfo(string Name, string? Detail, bool Exists, bool CanOpen);

/// <summary>The script slots of one spawn, as computed by the bridge on the UI thread.</summary>
public sealed class SpawnScriptsState
{
    public required SpawnScriptOwner Owner { get; init; }
    public required IReadOnlyList<SpawnScriptSlotInfo> Slots { get; init; }
}
