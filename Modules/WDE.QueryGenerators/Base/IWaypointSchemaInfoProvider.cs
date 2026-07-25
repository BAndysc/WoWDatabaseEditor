using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WDE.QueryGenerators.Base;

/// <summary>
/// Per-core waypoint schema details the editors need beyond the query providers — currently the
/// path-id column per waypoint table family (used by the "load path by id" picker's DISTINCT id
/// enumeration). One implementation per core family ([RequiresCore]), so editor code stays free of
/// core/schema conditionals.
/// </summary>
[NonUniqueProvider]
public interface IWaypointSchemaInfoProvider
{
    /// <summary>The column holding the path id (or creature guid for guid-keyed tables) in the
    /// family's table, or null when the family is unsupported on this core.</summary>
    string? PathIdColumn(WaypointTables table);

    /// <summary>The actual table name of the family on this core, for display (e.g. the
    /// WaypointData family is `waypoint_data` on Wrath/Cata but `waypoint_path` on master), or null
    /// when the family is unsupported on this core.</summary>
    string? TableName(WaypointTables table);

    /// <summary>Which optional columns the family's table ACTUALLY has on this core - the editor
    /// only offers fields that really save (e.g. velocity exists in waypoint_data on Cata but not
    /// on Wrath; move_type/action are per-point on Wrath but path-level on master). Position and
    /// the wait time/delay always exist and have no flag.</summary>
    WaypointColumns Columns(WaypointTables table);
}

/// <summary>Optional per-point columns + path-level extras a waypoint table may have.</summary>
[Flags]
public enum WaypointColumns
{
    None = 0,
    Orientation = 1,
    Velocity = 2,
    SmoothTransition = 4,
    MoveType = 8,
    Action = 16,
    ActionChance = 32,
    ScriptId = 64,
    Comment = 128,
    /// <summary>A path-level metadata row exists (TC master `waypoint_path`: MoveType, Flags,
    /// Velocity, Comment per path).</summary>
    PathHeader = 256,
    /// <summary>A path-level name row exists (CMaNGOS `waypoint_path_name`).</summary>
    PathName = 512,
}
