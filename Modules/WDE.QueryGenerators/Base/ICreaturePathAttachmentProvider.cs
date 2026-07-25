using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Base;

/// <summary>
/// Per-core "how is a waypoint path attached to a creature spawn": resolution, path id allocation and
/// the attach/detach SQL. One implementation is registered per core family ([RequiresCore]); cores
/// without creature-attached paths register none, and the editors hide the feature. This keeps ALL
/// core-specific knowledge (table/column names, id conventions) out of the editor code.
/// </summary>
[NonUniqueProvider]
public interface ICreaturePathAttachmentProvider
{
    /// <summary>The waypoint table family the attached paths live in (the editor maps it to its
    /// waypoint source).</summary>
    WaypointTables PathTable { get; }

    /// <summary>The path key currently attached to the creature, or null when it has none. Trinity:
    /// the addon path id. Mangos (guid-keyed creature_movement): the guid, but only when the
    /// creature's MovementType is a waypoint type — otherwise it "has" no path (an idle-movement
    /// creature with an empty table must read as unattached, not as an empty path).</summary>
    uint? ResolveAttachedPathId(uint creatureGuid, IBaseCreatureAddon? addon, MovementType movementType);

    /// <summary>The path key to use when attaching a new path to this creature.</summary>
    uint AllocatePathId(uint creatureGuid);

    /// <summary>SQL attaching the path to the creature and enabling waypoint movement. Must be
    /// idempotent (it also runs when opening an already-attached path for editing). Used only in
    /// headless hosts — the full editor uses <see cref="AttachFields"/> instead.</summary>
    IQuery Attach(uint creatureGuid, uint pathId);

    /// <summary>SQL detaching the creature from the path (movement back to idle). Must be idempotent.
    /// Used only in headless hosts — the full editor uses <see cref="DetachFields"/> instead.</summary>
    IQuery Detach(uint creatureGuid, uint pathId);

    /// <summary>The attachment expressed as creature-table document field updates — column names as
    /// the table editor sees them ("column", or "foreign_table.column" for columns flattened from a
    /// foreign table like creature_addon). The full editor applies these to its hosted creature
    /// document, so the attachment is save/undo/SESSION first-class instead of raw live SQL.</summary>
    IReadOnlyList<(string column, long value)> AttachFields(uint pathId);

    /// <summary>Field updates detaching the path (see <see cref="AttachFields"/>).</summary>
    IReadOnlyList<(string column, long value)> DetachFields();
}
