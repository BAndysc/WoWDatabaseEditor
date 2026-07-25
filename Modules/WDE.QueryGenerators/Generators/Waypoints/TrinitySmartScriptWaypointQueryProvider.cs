using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Waypoints;

// ISmartScriptWaypoint is stored in `waypoints` (entry == path id). Cata adds velocity +
// smoothTransition. Delete removes the whole path.

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth")]
public class TrinitySmartScriptWaypointQueryProvider : BaseInsertQueryProvider<ISmartScriptWaypoint>, IDeleteQueryProvider<ISmartScriptWaypoint>
{
    protected override object Convert(ISmartScriptWaypoint wp) => new
    {
        entry = wp.PathId,
        pointid = wp.PointId,
        position_x = wp.X,
        position_y = wp.Y,
        position_z = wp.Z,
        orientation = wp.Orientation,
        delay = wp.Delay,
        point_comment = wp.Comment,
    };

    public IQuery Delete(ISmartScriptWaypoint wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("entry") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoints");
}

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityCata")]
public class TrinityCataSmartScriptWaypointQueryProvider : BaseInsertQueryProvider<ISmartScriptWaypoint>, IDeleteQueryProvider<ISmartScriptWaypoint>
{
    protected override object Convert(ISmartScriptWaypoint wp) => new
    {
        entry = wp.PathId,
        pointid = wp.PointId,
        position_x = wp.X,
        position_y = wp.Y,
        position_z = wp.Z,
        orientation = wp.Orientation,
        delay = wp.Delay,
        point_comment = wp.Comment,
        velocity = wp.Velocity,
        smoothTransition = wp.SmoothTransition,
    };

    public IQuery Delete(ISmartScriptWaypoint wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("entry") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("waypoints");
}
