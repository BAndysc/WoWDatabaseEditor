using System.ComponentModel;
using Prism.Events;
using WDE.Common.Services.Mcp;
using WDE.MapSpawns.Models;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Mcp;

// ---- shared DTOs ---------------------------------------------------------------------------------

public sealed class PositionDto
{
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }

    public static PositionDto From(Vector3 v) => new() { X = v.X, Y = v.Y, Z = v.Z };
}

public sealed class SpawnEditStateDto
{
    [Description("True when there are unsaved pending spawn edits (spawn_save would write them).")]
    public required bool HasChanges { get; init; }
    public required bool CanUndo { get; init; }
    public required bool CanRedo { get; init; }
    [Description("Name of the operation spawn_undo would revert, null when the undo stack is empty.")]
    public string? NextUndo { get; init; }
    [Description("Name of the operation spawn_redo would re-apply, null when the redo stack is empty.")]
    public string? NextRedo { get; init; }
    [Description("Spawns queued for DELETE on the next spawn_save.")]
    public required int PendingDeleteCount { get; init; }
    [Description("Spawns created this session, pending until the next spawn_save.")]
    public required int NewSpawnCount { get; init; }

    public static SpawnEditStateDto From(IWorldSpawnEditService s) => new()
    {
        HasChanges = s.HasChanges,
        CanUndo = s.CanUndo,
        CanRedo = s.CanRedo,
        NextUndo = s.NextUndo,
        NextRedo = s.NextRedo,
        PendingDeleteCount = s.PendingDeletes.Count,
        NewSpawnCount = s.NewSpawns.Count
    };
}

// ---- game_view_status ----------------------------------------------------------------------------

public sealed class GameViewStatusOutput
{
    [Description("True when the 3D game view is open and its game loop is running.")]
    public required bool IsRunning { get; init; }
    [Description("Currently loaded map id, null when the 3D view is not running.")]
    public int? MapId { get; init; }
    [Description("Current camera position (WoW world coordinates), null when the 3D view is not running.")]
    public PositionDto? CameraPosition { get; init; }
    [Description("True when the spawn-edit bridge answers, i.e. the spawn_* editing tools are usable.")]
    public required bool SpawnEditingAvailable { get; init; }
    [Description("Pending spawn-edit state, null when spawn editing is unavailable.")]
    public SpawnEditStateDto? EditState { get; init; }
}

[AutoRegister]
[SingleInstance]
public class GameViewStatusTool : McpTool<EmptyInput, GameViewStatusOutput>
{
    private readonly Lazy<IEventAggregator> eventAggregator;
    private readonly Lazy<McpSpawnEditGateway> gateway;

    public GameViewStatusTool(Lazy<IEventAggregator> eventAggregator, Lazy<McpSpawnEditGateway> gateway)
    {
        this.eventAggregator = eventAggregator;
        this.gateway = gateway;
    }

    public override string Name => "game_view_status";
    public override string Description => "Reports the state of the editor's 3D game view: whether it is running, the loaded map id, the camera position and the pending spawn-edit state (unsaved changes, undo/redo, pending deletes, new spawns). Call this before other game_view/spawn tools to know what is possible.";

    protected override async Task<GameViewStatusOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var request = new GameViewInfoRequest();
        eventAggregator.Value.GetEvent<GameViewInfoRequestedEvent>().Publish(request);
        var (running, info) = await McpAwait.WaitFor(request.Result.Task, 1500, token);

        var available = await gateway.Value.WaitForAvailability(500, token);
        var service = gateway.Value.Service;
        return new GameViewStatusOutput
        {
            IsRunning = running,
            MapId = info?.MapId,
            CameraPosition = info == null ? null : PositionDto.From(info.CameraPosition),
            SpawnEditingAvailable = available,
            EditState = available ? SpawnEditStateDto.From(service) : null
        };
    }
}

// ---- camera_fly_to -------------------------------------------------------------------------------

public sealed class CameraFlyToInput
{
    [Description("Map id to show; when it differs from the currently loaded map, the view switches maps first.")]
    public required int Map { get; set; }
    [Description("Target X (WoW world coordinates).")]
    public required float X { get; set; }
    [Description("Target Y (WoW world coordinates).")]
    public required float Y { get; set; }
    [Description("Target Z (WoW world coordinates).")]
    public required float Z { get; set; }
    [Description("True (default) = smoothly fly/frame the target like the editor's own 'fly camera here'; false = land exactly on the position, instantly. A map switch always uses the fly-here framing.")]
    public bool Fly { get; set; } = true;
}

public sealed class CameraFlyToOutput
{
    public required bool MapChanged { get; init; }
    public required string Message { get; init; }
}

[AutoRegister]
[SingleInstance]
public class CameraFlyToTool : McpTool<CameraFlyToInput, CameraFlyToOutput>
{
    private readonly Lazy<IEventAggregator> eventAggregator;

    public CameraFlyToTool(Lazy<IEventAggregator> eventAggregator)
    {
        this.eventAggregator = eventAggregator;
    }

    public override string Name => "camera_fly_to";
    public override string Description => "Moves the 3D view camera to a world position, switching the loaded map first when needed. Requires the 3D game view to be open. Only moves the camera - nothing is modified in the database.";
    public override bool Mutating => true;

    protected override async Task<CameraFlyToOutput> Execute(CameraFlyToInput input, CancellationToken token)
    {
        var request = new CameraFlyToRequest
        {
            Map = input.Map,
            Position = new Vector3(input.X, input.Y, input.Z),
            Fly = input.Fly
        };
        eventAggregator.Value.GetEvent<CameraFlyToRequestedEvent>().Publish(request);
        var (completed, mapChanged) = await McpAwait.WaitFor(request.Done.Task, 5000, token);
        if (!completed)
            throw new McpToolException("The 3D game view is not open - open it (load a map in the editor's 3D view) before moving the camera.");
        return new CameraFlyToOutput
        {
            MapChanged = mapChanged,
            Message = mapChanged
                ? $"Switching to map {input.Map}; the camera will be placed at ({input.X}, {input.Y}, {input.Z}) once the map loads."
                : $"Camera relocated to ({input.X}, {input.Y}, {input.Z})."
        };
    }
}

// ---- spawn_selected ------------------------------------------------------------------------------

public sealed class SelectedSpawnDto
{
    [Description("\"creature\" or \"gameobject\".")]
    public required string Kind { get; init; }
    public required uint Guid { get; init; }
    public required uint Entry { get; init; }
    [Description("Template name of the spawned creature/gameobject.")]
    public required string Name { get; init; }
    public required int Map { get; init; }
    public required PositionDto Position { get; init; }
    public required float Orientation { get; init; }
    [Description("True when the spawn is queued for DELETE on the next spawn_save.")]
    public required bool PendingDelete { get; init; }
}

public sealed class SpawnSelectedOutput
{
    [Description("False when nothing is selected in the 3D view (spawn is null then).")]
    public required bool Selected { get; init; }
    public SelectedSpawnDto? Spawn { get; init; }
}

[AutoRegister]
[SingleInstance]
public class SpawnSelectedTool : McpTool<EmptyInput, SpawnSelectedOutput>
{
    private readonly Lazy<IEventAggregator> eventAggregator;
    private readonly Lazy<McpSpawnEditGateway> gateway;

    public SpawnSelectedTool(Lazy<IEventAggregator> eventAggregator, Lazy<McpSpawnEditGateway> gateway)
    {
        this.eventAggregator = eventAggregator;
        this.gateway = gateway;
    }

    public override string Name => "spawn_selected";
    public override string Description => "Returns the creature/gameobject spawn currently selected in the 3D game view (guid, entry, name, map, position), or selected=false when nothing is selected. Requires the 3D game view to be open.";

    protected override async Task<SpawnSelectedOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var request = new SelectedSpawnRequest();
        eventAggregator.Value.GetEvent<SelectedSpawnRequestedEvent>().Publish(request);
        var (completed, info) = await McpAwait.WaitFor(request.Result.Task, 3000, token);
        if (!completed)
            throw new McpToolException("The 3D game view is not open - open it (load a map in the editor's 3D view) to inspect the selection.");
        if (info == null)
            return new SpawnSelectedOutput { Selected = false };

        // give the lazily-built edit client a beat to receive the bridge's state, so PendingDelete
        // is read from real state, not the empty pre-handshake snapshot
        await gateway.Value.WaitForAvailability(500, token);
        return new SpawnSelectedOutput
        {
            Selected = true,
            Spawn = new SelectedSpawnDto
            {
                Kind = info.IsCreature ? "creature" : "gameobject",
                Guid = info.Guid,
                Entry = info.Entry,
                Name = info.Name,
                Map = info.MapId,
                Position = PositionDto.From(info.Position),
                Orientation = info.Orientation,
                PendingDelete = gateway.Value.Service.IsPendingDelete(info.IsCreature, info.Guid)
            }
        };
    }
}
