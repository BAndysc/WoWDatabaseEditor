using WDE.Common.Database;
using WDE.QueryGenerators.Base;
using TheMaths;

namespace WDE.MapSpawns.Models.Waypoints;

/// <summary>
/// One loaded waypoint path being edited. Holds the points as <see cref="UniversalWaypoint"/>
/// (the format-agnostic representation) plus dirty state. <see cref="Version"/> is bumped on any
/// geometry change so the renderer can skip work when nothing moved.
/// </summary>
public sealed class EditablePath
{
    public WaypointSource Source { get; }

    /// <summary>Path id, or the creature guid for <see cref="WaypointSource.MangosCreatureMovement"/>.
    /// For the compound-keyed <see cref="WaypointSource.MangosCreatureMovementTemplate"/> this is the
    /// creature entry (the secondary component is <see cref="Key2"/>).</summary>
    public uint Key { get; }

    /// <summary>The secondary key component for compound-keyed sources (the PathId of
    /// <see cref="WaypointSource.MangosCreatureMovementTemplate"/>); 0 for every single-key source.</summary>
    public uint Key2 { get; }

    /// <summary>Non-zero when auto-loaded for a selected creature (its guid), so it can be auto-unloaded.</summary>
    public uint AutoLoadedFromCreatureGuid { get; }

    public List<UniversalWaypoint> Points { get; } = new();

    /// <summary>Path-level metadata for sources that have a per-path header row (TC master's
    /// waypoint_path: MoveType/Flags/Velocity/Comment). Null when the source has none. Set at load
    /// time via <see cref="SetHeaderLoaded"/> (not dirtying); edits go through <see cref="UpdateHeader"/>.</summary>
    public WaypointPathHeader? Header { get; private set; }

    /// <summary>Path-level display name for sources with a name row (CMaNGOS waypoint_path_name).
    /// Null = the source has none / no row and never touched; "" = the user cleared it (Save
    /// removes the row).</summary>
    public string? PathName { get; private set; }

    public void SetHeaderLoaded(WaypointPathHeader header) => Header = header;

    public void UpdateHeader(WaypointPathHeader header)
    {
        Header = header;
        MarkDirty();
    }

    public void SetPathNameLoaded(string? name) => PathName = name;

    public void UpdatePathName(string? name)
    {
        PathName = name;
        MarkDirty();
    }

    public bool IsDirty { get; private set; }
    public int Version { get; private set; }

    /// <summary>Set once the path has been unloaded, so a still-queued pending load for it (see
    /// <see cref="WaypointEditorService.PumpPendingLoads"/>) doesn't resurrect it.</summary>
    public bool Unloaded { get; private set; }

    public void MarkUnloaded() => Unloaded = true;

    private readonly IWaypointSchemaInfoProvider? schema;

    // compound-keyed sources read as "entry X / path Y", single-key sources as "#key"
    private string KeyLabel => Source.UsesSecondaryKey() ? $"entry {Key} / path {Key2}" : $"#{Key}";

    public string DisplayName => AutoLoadedFromCreatureGuid != 0
        ? $"{Source.ToName(schema)} {KeyLabel} (creature {AutoLoadedFromCreatureGuid})"
        : $"{Source.ToName(schema)} {KeyLabel}";

    public EditablePath(WaypointSource source, uint key, IEnumerable<UniversalWaypoint>? points,
        uint autoLoadedFromCreatureGuid = 0, IWaypointSchemaInfoProvider? schema = null, uint key2 = 0)
    {
        Source = source;
        Key = key;
        Key2 = key2;
        AutoLoadedFromCreatureGuid = autoLoadedFromCreatureGuid;
        this.schema = schema;
        if (points != null)
            Points.AddRange(points);
        Reindex();
    }

    public void MarkDirty()
    {
        IsDirty = true;
        Version++;
    }

    public void ClearDirty() => IsDirty = false;

    /// <summary>Reassigns sequential 1-based PointIds (the convention shared by all the tables).</summary>
    public void Reindex()
    {
        for (int i = 0; i < Points.Count; ++i)
        {
            var p = Points[i];
            p.PointId = (uint)(i + 1);
            p.PathId = Key;
            p.PathId2 = Key2;
            Points[i] = p;
        }
    }

    public void SetPosition(int index, Vector3 pos)
    {
        if (index < 0 || index >= Points.Count)
            return;
        var p = Points[index];
        p.X = pos.X;
        p.Y = pos.Y;
        p.Z = pos.Z;
        Points[index] = p;
        MarkDirty();
    }

    public int Append(Vector3 pos)
    {
        Points.Add(new UniversalWaypoint { PathId = Key, PathId2 = Key2, X = pos.X, Y = pos.Y, Z = pos.Z });
        Reindex();
        MarkDirty();
        return Points.Count - 1;
    }

    /// <summary>Inserts a new point at <paramref name="index"/> (i.e. before the current point there).</summary>
    public void InsertAt(int index, Vector3 pos)
    {
        index = Math.Clamp(index, 0, Points.Count);
        Points.Insert(index, new UniversalWaypoint { PathId = Key, PathId2 = Key2, X = pos.X, Y = pos.Y, Z = pos.Z });
        Reindex();
        MarkDirty();
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Points.Count)
            return;
        Points.RemoveAt(index);
        Reindex();
        MarkDirty();
    }
}
