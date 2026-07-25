using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Waypoints;

// Creature-attached waypoint paths per core family:
// - Trinity: creature_addon.path_id (PathId on master) points to waypoint_data (waypoint_path_node
//   on master); a per-guid creature_addon row overrides creature_template_addon. Path id convention
//   is creature guid * 10. Waypoint movement = creature.MovementType 2.
// - CMaNGOS: creature_movement is keyed directly by the creature guid, only MovementType matters.

internal abstract class BaseTrinityCreaturePathAttachmentProvider : ICreaturePathAttachmentProvider
{
    protected abstract string AddonPathColumn { get; }

    public WaypointTables PathTable => WaypointTables.WaypointData;

    public uint? ResolveAttachedPathId(uint creatureGuid, IBaseCreatureAddon? addon, MovementType movementType) =>
        addon is { PathId: > 0 } ? addon.PathId : null;

    public uint AllocatePathId(uint creatureGuid) => creatureGuid * 10;

    public IQuery Attach(uint creatureGuid, uint pathId)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        multi.Table(DatabaseTable.WorldTable("creature_addon")).InsertIgnore(new { guid = creatureGuid });
        multi.Table(DatabaseTable.WorldTable("creature_addon"))
            .Where(row => row.Column<uint>("guid") == creatureGuid).Set(AddonPathColumn, pathId).Update();
        multi.Add(MovementTypeSql.Set(creatureGuid, 2));
        return multi.Close();
    }

    public IQuery Detach(uint creatureGuid, uint pathId)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        multi.Table(DatabaseTable.WorldTable("creature_addon")).InsertIgnore(new { guid = creatureGuid });
        multi.Table(DatabaseTable.WorldTable("creature_addon"))
            .Where(row => row.Column<uint>("guid") == creatureGuid).Set(AddonPathColumn, 0).Update();
        multi.Add(MovementTypeSql.Set(creatureGuid, 0));
        return multi.Close();
    }

    // the creature editor definitions flatten creature_addon as a foreign table, so the attachment
    // is expressible as plain document field updates
    public IReadOnlyList<(string column, long value)> AttachFields(uint pathId) => new (string, long)[]
    {
        ($"creature_addon.{AddonPathColumn}", pathId),
        ("MovementType", 2),
    };

    public IReadOnlyList<(string column, long value)> DetachFields() => new (string, long)[]
    {
        ($"creature_addon.{AddonPathColumn}", 0),
        ("MovementType", 0),
    };
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth", "TrinityCata")]
internal class TrinityCreaturePathAttachmentProvider : BaseTrinityCreaturePathAttachmentProvider
{
    protected override string AddonPathColumn => "path_id";
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
internal class TrinityMasterCreaturePathAttachmentProvider : BaseTrinityCreaturePathAttachmentProvider
{
    protected override string AddonPathColumn => "PathId";
}

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosCreaturePathAttachmentProvider : ICreaturePathAttachmentProvider
{
    public WaypointTables PathTable => WaypointTables.MangosCreatureMovement;

    // creature_movement is keyed by the creature guid itself, so the actual "has a path" signal is
    // the movement type - an idle creature with no rows must read as unattached, not as an empty path
    public uint? ResolveAttachedPathId(uint creatureGuid, IBaseCreatureAddon? addon, MovementType movementType) =>
        movementType is MovementType.Waypoint or MovementType.LinearPath ? creatureGuid : null;

    public uint AllocatePathId(uint creatureGuid) => creatureGuid;

    public IQuery Attach(uint creatureGuid, uint pathId) => MovementTypeSql.Set(creatureGuid, 2);

    public IQuery Detach(uint creatureGuid, uint pathId) => MovementTypeSql.Set(creatureGuid, 0);

    public IReadOnlyList<(string column, long value)> AttachFields(uint pathId) => new (string, long)[]
    {
        ("MovementType", 2),
    };

    public IReadOnlyList<(string column, long value)> DetachFields() => new (string, long)[]
    {
        ("MovementType", 0),
    };
}

internal static class MovementTypeSql
{
    public static IQuery Set(uint creatureGuid, int movementType) =>
        Queries.Table(DatabaseTable.WorldTable("creature"))
            .Where(row => row.Column<uint>("guid") == creatureGuid)
            .Set("MovementType", movementType)
            .Update();
}

// --- per-core waypoint schema info (path-id columns per table family) ---------------------------

internal abstract class BaseNonMasterTrinityWaypointSchemaInfoProvider : IWaypointSchemaInfoProvider
{
    public string? PathIdColumn(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => "id",
        WaypointTables.SmartScriptWaypoint => "entry",
        WaypointTables.ScriptWaypoint => "entry",
        _ => null,
    };

    public string? TableName(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => "waypoint_data",
        WaypointTables.SmartScriptWaypoint => "waypoints",
        WaypointTables.ScriptWaypoint => "script_waypoint",
        _ => null,
    };

    public abstract WaypointColumns Columns(WaypointTables table);
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth")]
internal class TrinityWaypointSchemaInfoProvider : BaseNonMasterTrinityWaypointSchemaInfoProvider
{
    // 3.3.5-family schemas: no velocity/smoothTransition anywhere (those are Cata+ columns)
    public override WaypointColumns Columns(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => WaypointColumns.Orientation | WaypointColumns.MoveType |
                                       WaypointColumns.Action | WaypointColumns.ActionChance,
        WaypointTables.SmartScriptWaypoint => WaypointColumns.Orientation | WaypointColumns.Comment,
        WaypointTables.ScriptWaypoint => WaypointColumns.Comment,
        _ => WaypointColumns.None,
    };
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityCata")]
internal class TrinityCataWaypointSchemaInfoProvider : BaseNonMasterTrinityWaypointSchemaInfoProvider
{
    public override WaypointColumns Columns(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => WaypointColumns.Orientation | WaypointColumns.MoveType |
                                       WaypointColumns.Action | WaypointColumns.ActionChance |
                                       WaypointColumns.Velocity | WaypointColumns.SmoothTransition,
        WaypointTables.SmartScriptWaypoint => WaypointColumns.Orientation | WaypointColumns.Comment |
                                              WaypointColumns.Velocity | WaypointColumns.SmoothTransition,
        WaypointTables.ScriptWaypoint => WaypointColumns.Comment,
        _ => WaypointColumns.None,
    };
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
internal class TrinityMasterWaypointSchemaInfoProvider : IWaypointSchemaInfoProvider
{
    public string? PathIdColumn(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => "PathId", // waypoint_path_node
        WaypointTables.SmartScriptWaypoint => "entry",
        WaypointTables.ScriptWaypoint => "entry",
        _ => null,
    };

    public string? TableName(WaypointTables table) => table switch
    {
        WaypointTables.WaypointData => "waypoint_path", // parent table; points live in waypoint_path_node
        WaypointTables.SmartScriptWaypoint => "waypoints",
        WaypointTables.ScriptWaypoint => "script_waypoint",
        _ => null,
    };

    public WaypointColumns Columns(WaypointTables table) => table switch
    {
        // per-point: position, orientation, delay only; move type/velocity/comment moved to the
        // per-path waypoint_path header row
        WaypointTables.WaypointData => WaypointColumns.Orientation | WaypointColumns.PathHeader,
        _ => WaypointColumns.None,
    };
}

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosWaypointSchemaInfoProvider : IWaypointSchemaInfoProvider
{
    public string? PathIdColumn(WaypointTables table) => table switch
    {
        WaypointTables.MangosWaypointPath => "PathId",
        WaypointTables.MangosCreatureMovement => "Id",
        WaypointTables.MangosCreatureMovementTemplate => "Entry", // primary of the (Entry, PathId) compound key
        WaypointTables.ScriptWaypoint => "entry",
        _ => null,
    };

    public string? TableName(WaypointTables table) => table switch
    {
        WaypointTables.MangosWaypointPath => "waypoint_path",
        WaypointTables.MangosCreatureMovement => "creature_movement",
        WaypointTables.MangosCreatureMovementTemplate => "creature_movement_template",
        WaypointTables.ScriptWaypoint => "script_waypoint",
        _ => null,
    };

    public WaypointColumns Columns(WaypointTables table) => table switch
    {
        WaypointTables.MangosWaypointPath => WaypointColumns.Orientation | WaypointColumns.ScriptId |
                                             WaypointColumns.Comment | WaypointColumns.PathName,
        WaypointTables.MangosCreatureMovement => WaypointColumns.Orientation | WaypointColumns.ScriptId |
                                                 WaypointColumns.Comment,
        WaypointTables.MangosCreatureMovementTemplate => WaypointColumns.Orientation | WaypointColumns.ScriptId |
                                                         WaypointColumns.Comment,
        _ => WaypointColumns.None,
    };
}
