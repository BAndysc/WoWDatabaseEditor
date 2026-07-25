using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Waypoints;

// CMaNGOS waypoint formats. Delete removes the whole path.

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class MangosWaypointPathQueryProvider : BaseInsertQueryProvider<IMangosWaypoint>, IDeleteQueryProvider<IMangosWaypoint>
{
    protected override object Convert(IMangosWaypoint wp) => new
    {
        PathId = wp.PathId,
        Point = wp.PointId,
        PositionX = wp.X,
        PositionY = wp.Y,
        PositionZ = wp.Z,
        Orientation = wp.Orientation,
        WaitTime = wp.WaitTime,
        ScriptId = wp.ScriptId,
        Comment = wp.Comment,
    };

    public IQuery Delete(IMangosWaypoint wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("PathId") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_path");
}

[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class MangosCreatureMovementQueryProvider : BaseInsertQueryProvider<IMangosCreatureMovement>, IDeleteQueryProvider<IMangosCreatureMovement>
{
    protected override object Convert(IMangosCreatureMovement wp) => new
    {
        Id = wp.Guid,
        Point = wp.PointId,
        PositionX = wp.X,
        PositionY = wp.Y,
        PositionZ = wp.Z,
        Orientation = wp.Orientation,
        WaitTime = wp.WaitTime,
        ScriptId = wp.ScriptId,
        Comment = wp.Comment,
    };

    public IQuery Delete(IMangosCreatureMovement wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("Id") == wp.Guid).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_movement");
}

// creature_movement_template is keyed by (Entry, PathId) - a compound path key. Delete removes the
// whole (Entry, PathId) path.
[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class MangosCreatureMovementTemplateQueryProvider : BaseInsertQueryProvider<IMangosCreatureMovementTemplate>, IDeleteQueryProvider<IMangosCreatureMovementTemplate>
{
    protected override object Convert(IMangosCreatureMovementTemplate wp) => new
    {
        Entry = wp.Entry,
        PathId = wp.PathId,
        Point = wp.PointId,
        PositionX = wp.X,
        PositionY = wp.Y,
        PositionZ = wp.Z,
        Orientation = wp.Orientation,
        WaitTime = wp.WaitTime,
        ScriptId = wp.ScriptId,
        Comment = wp.Comment,
    };

    public IQuery Delete(IMangosCreatureMovementTemplate wp) =>
        Queries.Table(TableName)
            .Where(row => row.Column<uint>("Entry") == wp.Entry && row.Column<uint>("PathId") == wp.PathId)
            .Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("creature_movement_template");
}

// waypoint_path_name: the optional per-path display name. Update = rewrite the one row
// (an empty name removes it).
[AutoRegister]
[SingleInstance]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
public class MangosWaypointPathNameQueryProvider : IUpdateQueryProvider<IMangosWaypointsPathName>
{
    public IQuery Update(IMangosWaypointsPathName pathName)
    {
        var multi = Queries.BeginTransaction(DataDatabaseType.World);
        multi.Table(TableName).Where(row => row.Column<uint>("PathId") == pathName.PathId).Delete();
        if (!string.IsNullOrWhiteSpace(pathName.Name))
            multi.Table(TableName).Insert(new { PathId = pathName.PathId, Name = pathName.Name });
        return multi.Close();
    }

    public DatabaseTable TableName => DatabaseTable.WorldTable("waypoint_path_name");
}
