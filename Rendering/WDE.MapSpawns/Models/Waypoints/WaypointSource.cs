using WDE.Common.CoreVersion;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawns.Models.Waypoints;

/// <summary>
/// The different waypoint storage formats. Each maps 1:1 to a <see cref="WaypointTables"/>
/// flag, so the editor only offers the ones the active core supports
/// (<see cref="IDatabaseFeatures.SupportedWaypoints"/>).
/// </summary>
public enum WaypointSource
{
    TrinityWaypointData,    // waypoint_data / waypoint_path_node (master)
    SmartScriptWaypoint,    // waypoints
    ScriptWaypoint,         // script_waypoint
    MangosWaypointPath,     // waypoint_path
    MangosCreatureMovement, // creature_movement (keyed by creature guid, not a path id)
    MangosCreatureMovementTemplate, // creature_movement_template (keyed by entry + pathId - compound)
}

public static class WaypointSources
{
    public static readonly WaypointSource[] All =
    {
        WaypointSource.TrinityWaypointData,
        WaypointSource.SmartScriptWaypoint,
        WaypointSource.ScriptWaypoint,
        WaypointSource.MangosWaypointPath,
        WaypointSource.MangosCreatureMovement,
        WaypointSource.MangosCreatureMovementTemplate,
    };

    public static WaypointTables ToFlag(this WaypointSource source) => source switch
    {
        WaypointSource.TrinityWaypointData => WaypointTables.WaypointData,
        WaypointSource.SmartScriptWaypoint => WaypointTables.SmartScriptWaypoint,
        WaypointSource.ScriptWaypoint => WaypointTables.ScriptWaypoint,
        WaypointSource.MangosWaypointPath => WaypointTables.MangosWaypointPath,
        WaypointSource.MangosCreatureMovement => WaypointTables.MangosCreatureMovement,
        WaypointSource.MangosCreatureMovementTemplate => WaypointTables.MangosCreatureMovementTemplate,
        _ => 0,
    };

    /// <summary>Core-blind fallback name. Prefer the schema-aware overload — table names differ per
    /// core within a family (WaypointData is `waypoint_data` on Wrath/Cata but `waypoint_path` on
    /// master); this one only says what the family is generally called.</summary>
    public static string ToName(this WaypointSource source) => source switch
    {
        WaypointSource.TrinityWaypointData => "waypoint_data",
        WaypointSource.SmartScriptWaypoint => "waypoints (SmartAI)",
        WaypointSource.ScriptWaypoint => "script_waypoint",
        WaypointSource.MangosWaypointPath => "waypoint_path (mangos)",
        WaypointSource.MangosCreatureMovement => "creature_movement",
        WaypointSource.MangosCreatureMovementTemplate => "creature_movement_template",
        _ => source.ToString(),
    };

    /// <summary>The source's ACTUAL table name on the active core (via the per-core
    /// <see cref="IWaypointSchemaInfoProvider"/>), falling back to the core-blind name.</summary>
    public static string ToName(this WaypointSource source, IWaypointSchemaInfoProvider? schema)
    {
        var table = schema?.TableName(source.ToFlag());
        if (table == null)
            return source.ToName();
        // `waypoints` alone doesn't say what it is - keep the SmartAI qualifier
        return source == WaypointSource.SmartScriptWaypoint ? $"{table} (SmartAI)" : table;
    }

    /// <summary>True if the source is keyed by a creature guid rather than a path id.</summary>
    public static bool IsKeyedByGuid(this WaypointSource source) =>
        source == WaypointSource.MangosCreatureMovement;

    /// <summary>True if the source needs a secondary key (a compound key). Only
    /// <see cref="WaypointSource.MangosCreatureMovementTemplate"/>, keyed by (entry, pathId): the
    /// primary key is the entry, the secondary key is the path id.</summary>
    public static bool UsesSecondaryKey(this WaypointSource source) =>
        source == WaypointSource.MangosCreatureMovementTemplate;

    public static bool IsSupported(this WaypointSource source, ICoreVersion core) =>
        (core.DatabaseFeatures.SupportedWaypoints & source.ToFlag()) != 0;
}
