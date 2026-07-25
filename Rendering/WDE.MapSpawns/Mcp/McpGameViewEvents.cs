using Prism.Events;

namespace WDE.MapSpawns.Mcp;

// UI -> game request events for the MCP tools. This mirrors WorldSpawnEditEvents (game -> UI bridge)
// in the opposite direction: an MCP tool (UI thread) publishes a request carrying a
// TaskCompletionSource, the game-side McpGameViewBridge dequeues it on the game loop (game state
// must never be touched from the UI thread) and completes it. When no game view is running nobody
// answers, so callers await with a timeout and report a clean "3D view is not open" error.

/// <summary>Snapshot of the running game view, taken on the game loop.</summary>
public sealed class GameViewInfo
{
    public required int MapId { get; init; }
    public required Vector3 CameraPosition { get; init; }
}

public sealed class GameViewInfoRequest
{
    public TaskCompletionSource<GameViewInfo> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class GameViewInfoRequestedEvent : PubSubEvent<GameViewInfoRequest> { }

/// <summary>Teleport/fly the camera, switching the loaded map first when it differs.</summary>
public sealed class CameraFlyToRequest
{
    public required int Map { get; init; }
    public required Vector3 Position { get; init; }
    /// <summary>True = glide/frame the target ("fly camera here"), false = land exactly, instantly.
    /// A map switch always uses the fly-here framing (that is what <c>SetMap</c> does).</summary>
    public required bool Fly { get; init; }
    /// <summary>Completed on the game loop once the relocation was applied; the value tells whether
    /// a map switch was needed.</summary>
    public TaskCompletionSource<bool> Done { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class CameraFlyToRequestedEvent : PubSubEvent<CameraFlyToRequest> { }

/// <summary>The currently selected spawn, extracted on the game loop.</summary>
public sealed class SelectedSpawnInfo
{
    public required bool IsCreature { get; init; }
    public required uint Guid { get; init; }
    public required uint Entry { get; init; }
    public required string Name { get; init; }
    public required int MapId { get; init; }
    public required Vector3 Position { get; init; }
    public required float Orientation { get; init; }
    /// <summary>True when the spawn currently has a live world instance (model loaded).</summary>
    public required bool IsSpawnedInWorld { get; init; }
}

public sealed class SelectedSpawnRequest
{
    /// <summary>Null result = the game view is running but nothing is selected.</summary>
    public TaskCompletionSource<SelectedSpawnInfo?> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

public sealed class SelectedSpawnRequestedEvent : PubSubEvent<SelectedSpawnRequest> { }
