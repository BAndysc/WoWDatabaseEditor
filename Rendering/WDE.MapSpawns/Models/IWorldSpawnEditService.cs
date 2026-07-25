using System.Collections.Generic;
using Prism.Events;
using TheMaths;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Models;

/// <summary>A creature/gameobject spawn added this session (not yet in the DB base data).</summary>
public readonly record struct PendingSpawn(bool IsCreature, uint Entry, uint Guid, int Map, Vector3 Position, float Orientation);

/// <summary>
/// The game-side surface for editing spawns (add/delete) with undo and a single Save. It is a thin
/// facade over the event bus: mutations are published as request events, and the current pending
/// state is cached from <see cref="WorldSpawnEditStateChangedEvent"/>. In the full editor a bridge
/// (referencing the Avalonia-coupled DatabaseEditors) subscribes on the UI thread, drives the hosted
/// creature/gameobject table documents (SQL + session + undo), and publishes state back. In headless
/// hosts (RenderingTester) nobody answers, so <see cref="IsAvailable"/> stays false and the UI hides
/// the edit affordances — no separate stub needed.
/// </summary>
[UniqueProvider]
public interface IWorldSpawnEditService
{
    bool IsAvailable { get; }
    int Revision { get; }
    bool HasChanges { get; }
    bool CanUndo { get; }
    bool CanRedo { get; }

    /// <inheritdoc cref="WorldSpawnEditState.NextUndo"/>
    string? NextUndo { get; }
    string? NextRedo { get; }

    bool IsPendingDelete(bool isCreature, uint guid);
    IReadOnlySet<(bool isCreature, uint guid)> PendingDeletes { get; }
    IReadOnlyList<PendingSpawn> NewSpawns { get; }

    /// <inheritdoc cref="WorldSpawnEditState.SaveCounter"/>
    int SaveCounter { get; }
    IReadOnlyList<(bool isCreature, uint guid)> LastSaveDeleted { get; }

    void ToggleDelete(bool isCreature, uint entry, uint guid, int map);
    void CreateSpawn(bool isCreature, uint entry, int map, Vector3 position, float orientation);

    /// <summary>Like <see cref="CreateSpawn"/>, but the new row is a full clone of the source
    /// spawn's DB row (respawn time, flags, flattened addon columns...) - only guid and transform
    /// differ.</summary>
    void DuplicateSpawn(bool isCreature, uint sourceGuid, uint entry, int map, Vector3 position, float orientation);

    /// <summary>Persists a spawn's transform. <paramref name="rotation"/> is the gameobject's full
    /// quaternion (rotation0-3) - pass it for gameobjects whenever known, or a tilt set by the rotate
    /// gizmo (or already present in the DB) would be silently flattened back to yaw; ignored for
    /// creatures (they only store yaw).</summary>
    void MoveSpawn(bool isCreature, uint guid, Vector3 position, float orientation, Quaternion? rotation = null);

    /// <summary>Sets spawn-row document fields (plain columns or foreign-table-flattened ones like
    /// "creature_addon.path_id") as a pending, undoable edit — persisted on Save, session-tracked.
    /// <paramref name="description"/> names the undo operation.</summary>
    void SetSpawnFields(bool isCreature, uint guid, IReadOnlyList<(string Column, long Value)> fields, string description);

    void Save();
    void Undo();
    void Redo();

    /// <summary>The exact SQL <see cref="Save"/> would execute right now for the pending spawn
    /// edits, or null when unavailable / nothing pending. Answered by the bridge asynchronously.</summary>
    System.Threading.Tasks.Task<string?> GenerateSaveQuery();
}

public class WorldSpawnEditClient : IWorldSpawnEditService
{
    private readonly IEventAggregator eventAggregator;
    private volatile WorldSpawnEditState state = WorldSpawnEditState.Empty;

    public WorldSpawnEditClient(IEventAggregator eventAggregator)
    {
        this.eventAggregator = eventAggregator;
        // keepSubscriberReferenceAlive: this is a singleton that lives for the app's lifetime
        eventAggregator.GetEvent<WorldSpawnEditStateChangedEvent>()
            .Subscribe(s => state = s, ThreadOption.PublisherThread, keepSubscriberReferenceAlive: true);
        // the bridge usually activates before this client is built (it opens with the map view), so its
        // initial "available" state was already emitted - ask it to re-publish now that we're listening.
        eventAggregator.GetEvent<WorldSpawnEditStateRequestedEvent>().Publish();
    }

    public bool IsAvailable => state.Available;
    public int Revision => state.Revision;
    public bool HasChanges => state.HasChanges;
    public bool CanUndo => state.CanUndo;
    public bool CanRedo => state.CanRedo;
    public string? NextUndo => state.NextUndo;
    public string? NextRedo => state.NextRedo;

    public bool IsPendingDelete(bool isCreature, uint guid) => state.PendingDeletes.Contains((isCreature, guid));
    public IReadOnlySet<(bool isCreature, uint guid)> PendingDeletes => state.PendingDeletes;
    public IReadOnlyList<PendingSpawn> NewSpawns => state.NewSpawns;
    public int SaveCounter => state.SaveCounter;
    public IReadOnlyList<(bool isCreature, uint guid)> LastSaveDeleted => state.LastSaveDeleted;

    public void ToggleDelete(bool isCreature, uint entry, uint guid, int map) =>
        eventAggregator.GetEvent<SpawnDeleteToggleRequestedEvent>().Publish(new SpawnEditRef(isCreature, entry, guid, map));

    public void CreateSpawn(bool isCreature, uint entry, int map, Vector3 position, float orientation) =>
        eventAggregator.GetEvent<SpawnCreateRequestedEvent>().Publish(new SpawnCreateRequest(isCreature, entry, map, position, orientation));

    public void DuplicateSpawn(bool isCreature, uint sourceGuid, uint entry, int map, Vector3 position, float orientation) =>
        eventAggregator.GetEvent<SpawnDuplicateRequestedEvent>().Publish(new SpawnDuplicateRequest(isCreature, sourceGuid, entry, map, position, orientation));

    public void MoveSpawn(bool isCreature, uint guid, Vector3 position, float orientation, Quaternion? rotation = null) =>
        eventAggregator.GetEvent<SpawnMoveRequestedEvent>().Publish(new SpawnMoveRequest(isCreature, guid, position, orientation, isCreature ? null : rotation));

    public void SetSpawnFields(bool isCreature, uint guid, IReadOnlyList<(string Column, long Value)> fields, string description) =>
        eventAggregator.GetEvent<SpawnFieldsUpdateRequestedEvent>().Publish(new SpawnFieldsUpdateRequest(isCreature, guid, fields, description));

    public void Save() => eventAggregator.GetEvent<WorldEditCommandRequestedEvent>().Publish(WorldEditCommand.Save);
    public void Undo() => eventAggregator.GetEvent<WorldEditCommandRequestedEvent>().Publish(WorldEditCommand.Undo);
    public void Redo() => eventAggregator.GetEvent<WorldEditCommandRequestedEvent>().Publish(WorldEditCommand.Redo);

    public System.Threading.Tasks.Task<string?> GenerateSaveQuery()
    {
        if (!IsAvailable || !HasChanges)
            return System.Threading.Tasks.Task.FromResult<string?>(null);
        var request = new WorldEditGenerateQueryRequest();
        eventAggregator.GetEvent<WorldEditGenerateQueryRequestedEvent>().Publish(request);
        return request.Result.Task;
    }
}
