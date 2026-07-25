using System.Collections.Generic;
using Prism.Events;
using TheMaths;

namespace WDE.MapSpawns.Models;

// --- requests: published by the game side, applied by the document bridge (full app only) ---

/// <summary>Toggle a spawn's pending-delete state.</summary>
public class SpawnDeleteToggleRequestedEvent : PubSubEvent<SpawnEditRef> { }

/// <summary>Add a new spawn of the given template at a position (bridge allocates the guid).</summary>
public class SpawnCreateRequestedEvent : PubSubEvent<SpawnCreateRequest> { }

/// <summary>Duplicate an existing spawn at a new position: the bridge clones the source's FULL
/// database row (respawn time, flags, flattened addon columns...) under a freshly allocated guid,
/// only overriding the transform.</summary>
public class SpawnDuplicateRequestedEvent : PubSubEvent<SpawnDuplicateRequest> { }

/// <summary>A spawn was moved in the world (drag) — persist the new transform.</summary>
public class SpawnMoveRequestedEvent : PubSubEvent<SpawnMoveRequest> { }

/// <summary>Set arbitrary spawn-row document fields — plain columns or ones flattened from a foreign
/// table ("creature_addon.path_id"). Used e.g. for attaching/detaching creature waypoint paths, so
/// those edits are save/undo/session first-class like any other spawn edit.</summary>
public class SpawnFieldsUpdateRequestedEvent : PubSubEvent<SpawnFieldsUpdateRequest> { }

/// <summary>Global pending-changes command (one Save / Undo / Redo across the hosted documents).</summary>
public class WorldEditCommandRequestedEvent : PubSubEvent<WorldEditCommand> { }

/// <summary>Game -> bridge: build the exact SQL a Save would execute right now for the pending
/// spawn edits (no execution). The bridge answers by completing <see cref="Result"/>.</summary>
public class WorldEditGenerateQueryRequest
{
    public System.Threading.Tasks.TaskCompletionSource<string?> Result { get; } =
        new(System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
}

public class WorldEditGenerateQueryRequestedEvent : PubSubEvent<WorldEditGenerateQueryRequest> { }

/// <summary>Published by the game-side client when it comes online, asking the bridge (if any) to
/// re-publish the current state. Fixes the startup ordering: the bridge's one-time initial state is
/// emitted before the client exists, so the client pulls it on construction instead.</summary>
public class WorldSpawnEditStateRequestedEvent : PubSubEvent { }

// --- state: published back by the bridge, cached by the game for rendering + toolbar ---

public class WorldSpawnEditStateChangedEvent : PubSubEvent<WorldSpawnEditState> { }

/// <summary>Published by the bridge alongside its status-bar notifications so results the user must
/// not miss (spawn created, save succeeded/FAILED) are also visible inside the 3D view as toasts
/// (<see cref="IGameNotificationService"/>). The status bar alone is outside the user's focus
/// while world editing.</summary>
public class WorldSpawnEditNotificationEvent : PubSubEvent<WorldSpawnEditNotification> { }

public readonly record struct WorldSpawnEditNotification(bool Success, string Message);

public enum WorldEditCommand { Save, Undo, Redo }

public readonly record struct SpawnEditRef(bool IsCreature, uint Entry, uint Guid, int Map);

public readonly record struct SpawnCreateRequest(bool IsCreature, uint Entry, int Map, Vector3 Position, float Orientation);

public readonly record struct SpawnDuplicateRequest(bool IsCreature, uint SourceGuid, uint Entry, int Map, Vector3 Position, float Orientation);

/// <summary>Rotation: the gameobject's full quaternion (rotation0-3 columns), null for creatures
/// (or when only yaw is known - the bridge then derives rotation2/3 from the yaw).</summary>
public readonly record struct SpawnMoveRequest(bool IsCreature, uint Guid, Vector3 Position, float Orientation, Quaternion? Rotation = null);

public readonly record struct SpawnFieldsUpdateRequest(bool IsCreature, uint Guid,
    IReadOnlyList<(string Column, long Value)> Fields, string Description);

/// <summary>Immutable snapshot of the document bridge's pending state (single source of truth).</summary>
public sealed class WorldSpawnEditState
{
    public bool Available { get; init; }
    public IReadOnlySet<(bool isCreature, uint guid)> PendingDeletes { get; init; } = System.Collections.Immutable.ImmutableHashSet<(bool, uint)>.Empty;
    public IReadOnlyList<PendingSpawn> NewSpawns { get; init; } = System.Array.Empty<PendingSpawn>();
    public bool HasChanges { get; init; }
    public bool CanUndo { get; init; }
    public bool CanRedo { get; init; }

    /// <summary>Human-readable name of the operation Undo/Redo would apply ("Move spawn"...), null
    /// when the stack is empty. Undo/redo cover ONLY spawn edits - naming the operation in the UI
    /// is what keeps that scope honest while a non-spawn tool is active.</summary>
    public string? NextUndo { get; init; }
    public string? NextRedo { get; init; }

    public int Revision { get; init; }

    /// <summary>Bumped once per successful Save. When the game sees it change, the spawns listed in
    /// <see cref="LastSaveDeleted"/> were committed as DELETEs (remove their world instances), and any
    /// previously pending <see cref="NewSpawns"/> became real DB rows (keep their instances).</summary>
    public int SaveCounter { get; init; }
    public IReadOnlyList<(bool isCreature, uint guid)> LastSaveDeleted { get; init; } = System.Array.Empty<(bool, uint)>();

    public static readonly WorldSpawnEditState Empty = new();
}
