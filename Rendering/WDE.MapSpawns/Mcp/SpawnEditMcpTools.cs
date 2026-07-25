using System.ComponentModel;
using WDE.Common.Services.Mcp;
using WDE.MapSpawns.Models;
using WDE.Module.Attributes;

namespace WDE.MapSpawns.Mcp;

// The spawn_* editing tools drive the same event-bus facade the 3D view's own editing UI uses
// (IWorldSpawnEditService -> WorldSpawnEditBridge over hosted table documents): every mutation is a
// pending, undoable edit - NOTHING touches the database until spawn_save. The mutation events are
// fire-and-forget and served asynchronously on the UI thread, so after publishing, the tools wait
// for the bridge's next state publish (Revision bump) to return a fresh snapshot.

public sealed class SpawnEditOpOutput
{
    public required string Message { get; init; }
    public required SpawnEditStateDto State { get; init; }
}

internal static class SpawnEditHelpers
{
    /// <summary>The guid of the pending new spawn that was not in <paramref name="known"/> before the
    /// create/duplicate request - i.e. the guid the bridge just allocated.</summary>
    public static uint? FindNewGuid(IWorldSpawnEditService service, bool isCreature, HashSet<(bool IsCreature, uint Guid)> known)
    {
        foreach (var s in service.NewSpawns)
        {
            if (s.IsCreature == isCreature && !known.Contains((s.IsCreature, s.Guid)))
                return s.Guid;
        }
        return null;
    }
}

// ---- spawn_create --------------------------------------------------------------------------------

public sealed class SpawnCreateInput
{
    [Description("True to spawn a creature, false to spawn a gameobject.")]
    public required bool IsCreature { get; set; }
    [Description("creature_template/gameobject_template entry of the object to spawn.")]
    public required uint Entry { get; set; }
    [Description("Map id to spawn on.")]
    public required int Map { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Z { get; set; }
    [Description("Facing in radians (default 0).")]
    public float Orientation { get; set; }
}

public sealed class SpawnCreateOutput
{
    [Description("Guid the editor allocated for the new pending spawn, null when the editor did not confirm in time.")]
    public uint? Guid { get; init; }
    public required string Message { get; init; }
    public required SpawnEditStateDto State { get; init; }
}

[AutoRegister]
[SingleInstance]
public class SpawnCreateTool : McpTool<SpawnCreateInput, SpawnCreateOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnCreateTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_create";
    public override string Description => "Creates a new creature/gameobject spawn at a world position as a PENDING, undoable edit in the 3D spawn editor (the editor allocates the guid; nothing is written to the database until spawn_save). Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnCreateOutput> Execute(SpawnCreateInput input, CancellationToken token)
    {
        if (input.Entry == 0)
            throw new McpToolException("Entry must be a valid creature_template/gameobject_template entry (non-zero).");
        var service = await gateway.Value.RequireAvailable(token);
        var revision = service.Revision;
        var known = service.NewSpawns.Select(s => (s.IsCreature, s.Guid)).ToHashSet();

        service.CreateSpawn(input.IsCreature, input.Entry, input.Map,
            new Vector3(input.X, input.Y, input.Z), input.Orientation);

        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        uint? guid = confirmed ? SpawnEditHelpers.FindNewGuid(service, input.IsCreature, known) : null;
        return new SpawnCreateOutput
        {
            Guid = guid,
            Message = confirmed
                ? $"Pending spawn created{(guid.HasValue ? $" with guid {guid}" : "")}. Run spawn_save to persist it."
                : "Spawn creation was requested but the editor did not confirm within 3 s - check game_view_status (the entry may be unknown).",
            State = SpawnEditStateDto.From(service)
        };
    }
}

// ---- spawn_duplicate -----------------------------------------------------------------------------

public sealed class SpawnDuplicateInput
{
    [Description("True when the source spawn is a creature, false for a gameobject.")]
    public required bool IsCreature { get; set; }
    [Description("Guid of the existing spawn to clone.")]
    public required uint SourceGuid { get; set; }
    [Description("Entry of the source spawn (creature/gameobject table entry column).")]
    public required uint Entry { get; set; }
    [Description("Map id of the source spawn (the clone is placed on the same map).")]
    public required int Map { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Z { get; set; }
    [Description("Facing of the clone in radians (default 0).")]
    public float Orientation { get; set; }
}

[AutoRegister]
[SingleInstance]
public class SpawnDuplicateTool : McpTool<SpawnDuplicateInput, SpawnCreateOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnDuplicateTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_duplicate";
    public override string Description => "Duplicates an existing spawn at a new position: the new row is a full clone of the source spawn's database row (respawn time, flags, flattened addon columns...) - only guid and transform differ. A pending, undoable edit; nothing is written until spawn_save. Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnCreateOutput> Execute(SpawnDuplicateInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        var revision = service.Revision;
        var known = service.NewSpawns.Select(s => (s.IsCreature, s.Guid)).ToHashSet();

        service.DuplicateSpawn(input.IsCreature, input.SourceGuid, input.Entry, input.Map,
            new Vector3(input.X, input.Y, input.Z), input.Orientation);

        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        uint? guid = confirmed ? SpawnEditHelpers.FindNewGuid(service, input.IsCreature, known) : null;
        return new SpawnCreateOutput
        {
            Guid = guid,
            Message = confirmed
                ? $"Pending duplicate of {(input.IsCreature ? "creature" : "gameobject")} {input.SourceGuid} created{(guid.HasValue ? $" with guid {guid}" : "")}. Run spawn_save to persist it."
                : "Duplication was requested but the editor did not confirm within 3 s - check game_view_status (does the source guid exist?).",
            State = SpawnEditStateDto.From(service)
        };
    }
}

// ---- spawn_move ----------------------------------------------------------------------------------

public sealed class SpawnMoveInput
{
    [Description("True for a creature spawn, false for a gameobject spawn.")]
    public required bool IsCreature { get; set; }
    [Description("Guid of the spawn to move.")]
    public required uint Guid { get; set; }
    public required float X { get; set; }
    public required float Y { get; set; }
    public required float Z { get; set; }
    [Description("New facing in radians. Note: for gameobjects this rewrites the full rotation quaternion from the yaw, so any tilt stored in rotation0-3 is flattened.")]
    public required float Orientation { get; set; }
}

[AutoRegister]
[SingleInstance]
public class SpawnMoveTool : McpTool<SpawnMoveInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnMoveTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_move";
    public override string Description => "Moves an existing spawn to a new position/orientation as a pending, undoable edit (persisted on spawn_save). Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(SpawnMoveInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        var revision = service.Revision;
        service.MoveSpawn(input.IsCreature, input.Guid, new Vector3(input.X, input.Y, input.Z), input.Orientation);
        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        return new SpawnEditOpOutput
        {
            Message = confirmed
                ? $"Pending move of {(input.IsCreature ? "creature" : "gameobject")} {input.Guid} to ({input.X}, {input.Y}, {input.Z}) recorded. Run spawn_save to persist."
                : "Move was requested but the editor did not confirm within 3 s - check game_view_status (does the guid exist?).",
            State = SpawnEditStateDto.From(service)
        };
    }
}

// ---- spawn_toggle_delete -------------------------------------------------------------------------

public sealed class SpawnToggleDeleteInput
{
    [Description("True for a creature spawn, false for a gameobject spawn.")]
    public required bool IsCreature { get; set; }
    [Description("Guid of the spawn whose pending-delete state to toggle.")]
    public required uint Guid { get; set; }
    [Description("Entry of the spawn (creature/gameobject table entry column) - required to locate its row.")]
    public required uint Entry { get; set; }
    [Description("Map id the spawn lives on.")]
    public required int Map { get; set; }
}

[AutoRegister]
[SingleInstance]
public class SpawnToggleDeleteTool : McpTool<SpawnToggleDeleteInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnToggleDeleteTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_toggle_delete";
    public override string Description => "Toggles a spawn's pending-delete flag: first call queues the spawn for DELETE on the next spawn_save, calling again un-queues it. A pending, undoable edit; nothing is deleted until spawn_save. Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(SpawnToggleDeleteInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        var revision = service.Revision;
        var wasPending = service.IsPendingDelete(input.IsCreature, input.Guid);
        service.ToggleDelete(input.IsCreature, input.Entry, input.Guid, input.Map);
        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        var isPending = service.IsPendingDelete(input.IsCreature, input.Guid);
        return new SpawnEditOpOutput
        {
            Message = confirmed
                ? isPending
                    ? $"{(input.IsCreature ? "Creature" : "Gameobject")} {input.Guid} is now queued for DELETE on the next spawn_save."
                    : $"{(input.IsCreature ? "Creature" : "Gameobject")} {input.Guid} is no longer queued for deletion."
                : $"Toggle was requested but the editor did not confirm within 3 s (pending-delete was {wasPending}) - check game_view_status.",
            State = SpawnEditStateDto.From(service)
        };
    }
}

// ---- spawn_set_fields ----------------------------------------------------------------------------

public sealed class SpawnSetFieldsInput
{
    [Description("True for a creature spawn, false for a gameobject spawn.")]
    public required bool IsCreature { get; set; }
    [Description("Guid of the spawn to edit.")]
    public required uint Guid { get; set; }
    [Description("Column name -> new integer value. Keys are the spawn table's column names exactly as the 3D inspector shows them (e.g. \"spawntimesecs\", \"MovementType\") or foreign-table-flattened columns like \"creature_addon.path_id\".")]
    public required Dictionary<string, long> Fields { get; set; }
    [Description("Optional name for the undo entry (e.g. \"Set respawn time\").")]
    public string? Description { get; set; }
}

[AutoRegister]
[SingleInstance]
public class SpawnSetFieldsTool : McpTool<SpawnSetFieldsInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnSetFieldsTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_set_fields";
    public override string Description => "Sets integer columns of a spawn's database row (the columns the 3D spawn inspector shows, incl. foreign-table-flattened ones like creature_addon.path_id) as a pending, undoable edit - persisted on spawn_save. Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(SpawnSetFieldsInput input, CancellationToken token)
    {
        if (input.Fields.Count == 0)
            throw new McpToolException("Fields must contain at least one column -> value pair.");
        var service = await gateway.Value.RequireAvailable(token);
        var revision = service.Revision;
        var fields = input.Fields.Select(kv => (kv.Key, kv.Value)).ToList();
        service.SetSpawnFields(input.IsCreature, input.Guid, fields,
            string.IsNullOrWhiteSpace(input.Description) ? "Set spawn fields (MCP)" : input.Description!);
        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        return new SpawnEditOpOutput
        {
            Message = confirmed
                ? $"Pending edit of {input.Fields.Count} field(s) on {(input.IsCreature ? "creature" : "gameobject")} {input.Guid} recorded. Run spawn_save to persist."
                : "Field update was requested but the editor did not confirm within 3 s - check game_view_status (do the guid and columns exist?).",
            State = SpawnEditStateDto.From(service)
        };
    }
}

// ---- spawn_save / spawn_undo / spawn_redo / spawn_generate_sql -----------------------------------

[AutoRegister]
[SingleInstance]
public class SpawnSaveTool : McpTool<EmptyInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnSaveTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_save";
    public override string Description => "Executes the pending 3D spawn edits' Save: writes all pending spawn creations/moves/field edits/deletes to the world database in one go (use spawn_generate_sql first to preview the exact SQL). Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        if (!service.HasChanges)
            return new SpawnEditOpOutput { Message = "No pending spawn changes to save.", State = SpawnEditStateDto.From(service) };

        var saveCounter = service.SaveCounter;
        var notifBefore = gateway.Value.LastNotification?.Seq ?? 0;
        service.Save();

        var deadline = Environment.TickCount64 + 15000;
        while (Environment.TickCount64 < deadline)
        {
            if (service.SaveCounter != saveCounter)
            {
                var notif = gateway.Value.LastNotification;
                var detail = notif != null && notif.Seq > notifBefore ? $" ({notif.Message})" : "";
                return new SpawnEditOpOutput
                {
                    Message = $"Saved successfully{detail}.",
                    State = SpawnEditStateDto.From(service)
                };
            }
            var failure = gateway.Value.LastNotification;
            if (failure != null && failure.Seq > notifBefore && !failure.Success)
                throw new McpToolException($"Save failed: {failure.Message}");
            await Task.Delay(100, token);
        }
        return new SpawnEditOpOutput
        {
            Message = "Save was requested but not confirmed within 15 s - check the editor's status bar and game_view_status before retrying.",
            State = SpawnEditStateDto.From(service)
        };
    }
}

[AutoRegister]
[SingleInstance]
public class SpawnUndoTool : McpTool<EmptyInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnUndoTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_undo";
    public override string Description => "Undoes the most recent pending 3D spawn edit (the undo stack covers only spawn edits, named in game_view_status as nextUndo). Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        if (!service.CanUndo)
            throw new McpToolException("Nothing to undo in the 3D spawn editor.");
        var name = service.NextUndo;
        var revision = service.Revision;
        service.Undo();
        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        return new SpawnEditOpOutput
        {
            Message = confirmed ? $"Undone: {name ?? "spawn edit"}." : "Undo was requested but not confirmed within 3 s - check game_view_status.",
            State = SpawnEditStateDto.From(service)
        };
    }
}

[AutoRegister]
[SingleInstance]
public class SpawnRedoTool : McpTool<EmptyInput, SpawnEditOpOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnRedoTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_redo";
    public override string Description => "Re-applies the most recently undone pending 3D spawn edit (named in game_view_status as nextRedo). Requires the 3D game view to be open.";
    public override bool Mutating => true;

    protected override async Task<SpawnEditOpOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        if (!service.CanRedo)
            throw new McpToolException("Nothing to redo in the 3D spawn editor.");
        var name = service.NextRedo;
        var revision = service.Revision;
        service.Redo();
        var confirmed = await McpAwait.WaitForRevisionChange(service, revision, 3000, token);
        return new SpawnEditOpOutput
        {
            Message = confirmed ? $"Redone: {name ?? "spawn edit"}." : "Redo was requested but not confirmed within 3 s - check game_view_status.",
            State = SpawnEditStateDto.From(service)
        };
    }
}

public sealed class SpawnGenerateSqlOutput
{
    [Description("The exact SQL spawn_save would execute right now, null when there are no pending spawn edits.")]
    public string? Sql { get; init; }
    public required string Message { get; init; }
}

[AutoRegister]
[SingleInstance]
public class SpawnGenerateSqlTool : McpTool<EmptyInput, SpawnGenerateSqlOutput>
{
    private readonly Lazy<McpSpawnEditGateway> gateway;
    public SpawnGenerateSqlTool(Lazy<McpSpawnEditGateway> gateway) => this.gateway = gateway;

    public override string Name => "spawn_generate_sql";
    public override string Description => "Returns the exact SQL the pending 3D spawn edits' spawn_save would execute right now, without executing anything. Requires the 3D game view to be open.";

    protected override async Task<SpawnGenerateSqlOutput> Execute(EmptyInput input, CancellationToken token)
    {
        var service = await gateway.Value.RequireAvailable(token);
        if (!service.HasChanges)
            return new SpawnGenerateSqlOutput { Sql = null, Message = "No pending spawn edits - nothing to generate." };
        var (completed, sql) = await McpAwait.WaitFor(service.GenerateSaveQuery(), 10000, token);
        if (!completed)
            throw new McpToolException("The editor did not answer the query-generation request within 10 s.");
        return new SpawnGenerateSqlOutput
        {
            Sql = sql,
            Message = string.IsNullOrWhiteSpace(sql) ? "No pending spawn edits - nothing to generate." : "This SQL is exactly what spawn_save would execute now."
        };
    }
}
