using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Waypoints;

// IWaypointData is stored in `waypoint_data` on Wrath/Azeroth/Cata (Cata adds velocity +
// smoothTransition) and in `waypoint_path_node` on TrinityMaster. The Delete provider deletes
// the WHOLE path (all points sharing the path id) - the waypoint editor saves a path by
// deleting it and bulk-inserting the current points.

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth")]
public class TrinityWaypointDataQueryProvider : BaseInsertQueryProvider<IWaypointData>, IDeleteQueryProvider<IWaypointData>
{
    protected override object Convert(IWaypointData wp) => new
    {
        id = wp.PathId,
        point = wp.PointId,
        position_x = wp.X,
        position_y = wp.Y,
        position_z = wp.Z,
        orientation = wp.Orientation,
        delay = wp.Delay,
        move_type = wp.MoveType,
        action = wp.Action,
        action_chance = wp.ActionChance,
    };

    public IQuery Delete(IWaypointData wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("id") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_data");
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityCata")]
public class TrinityCataWaypointDataQueryProvider : BaseInsertQueryProvider<IWaypointData>, IDeleteQueryProvider<IWaypointData>
{
    protected override object Convert(IWaypointData wp) => new
    {
        id = wp.PathId,
        point = wp.PointId,
        position_x = wp.X,
        position_y = wp.Y,
        position_z = wp.Z,
        orientation = wp.Orientation,
        delay = wp.Delay,
        move_type = wp.MoveType,
        action = wp.Action,
        action_chance = wp.ActionChance,
        // NOT NULL columns with a 0 default on TCPP - coalesce, never write NULL
        velocity = wp.Velocity ?? 0,
        smoothTransition = wp.SmoothTransition ?? false,
    };

    public IQuery Delete(IWaypointData wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("id") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_data");
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
public class TrinityMasterWaypointDataQueryProvider : BaseInsertQueryProvider<IWaypointData>, IDeleteQueryProvider<IWaypointData>, IDeleteAllQueryProvider<IWaypointData>
{
    protected override object Convert(IWaypointData wp) => new
    {
        PathId = wp.PathId,
        NodeId = wp.PointId,
        PositionX = wp.X,
        PositionY = wp.Y,
        PositionZ = wp.Z,
        Orientation = wp.Orientation,
        Delay = wp.Delay,
    };

    // waypoint_path_node rows need their waypoint_path parent row on master; inserts ensure it
    // (INSERT IGNORE keeps existing path metadata like MoveType/Flags), deletes keep it - a path
    // rewrite (delete + insert) must not lose the metadata.
    public override IQuery Insert(IWaypointData wp)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        EnsureParentPath(multi, wp.PathId);
        multi.Add(base.Insert(wp));
        return multi.Close();
    }

    public override IQuery BulkInsert(IReadOnlyCollection<IWaypointData> collection)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        foreach (var pathId in collection.Select(c => c.PathId).Distinct())
            EnsureParentPath(multi, pathId);
        multi.Add(base.BulkInsert(collection));
        return multi.Close();
    }

    private static void EnsureParentPath(IMultiQuery multi, uint pathId) =>
        multi.Table(DatabaseTable.WorldTable("waypoint_path")).InsertIgnore(new { PathId = pathId });

    public IQuery Delete(IWaypointData wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("PathId") == wp.PathId).Delete();

    /// <summary>Deletes the WHOLE path including the waypoint_path metadata parent row — for removing
    /// a path entirely (a rewrite uses Delete + Insert, which keeps the metadata).</summary>
    public IQuery DeleteAll(IWaypointData wp)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        multi.Add(Delete(wp));
        multi.Table(DatabaseTable.WorldTable("waypoint_path"))
            .Where(row => row.Column<uint>("PathId") == wp.PathId).Delete();
        return multi.Close();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_path_node");
}

// The per-path waypoint_path metadata row on master (MoveType/Flags/Velocity/Comment). Update =
// upsert: ensure the row exists (bare INSERT IGNORE, same as the node inserts do), then write the
// edited values - never REPLACE, which would momentarily drop the row other rows point at.
[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityMaster")]
public class TrinityMasterWaypointPathHeaderQueryProvider : IUpdateQueryProvider<IWaypointPathHeader>
{
    public IQuery Update(IWaypointPathHeader header)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        multi.Table(TableName).InsertIgnore(new { PathId = header.PathId });
        multi.Add(Queries.Table(TableName)
            .Where(row => row.Column<uint>("PathId") == header.PathId)
            .Set("MoveType", header.MoveType)
            .Set("Flags", header.Flags)
            .Set("Velocity", header.Velocity)
            .Set("Comment", header.Comment)
            .Update());
        return multi.Close();
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_path");
}
