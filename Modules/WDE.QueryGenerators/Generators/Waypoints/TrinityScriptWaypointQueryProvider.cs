using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.Waypoints;

// IScriptWaypoint is stored in `script_waypoint` (entry == path id). Delete removes the whole path.

[AutoRegister]
[SingleInstance]
[RequiresCore("TrinityWrath", "Azeroth", "TrinityCata")]
public class TrinityScriptWaypointQueryProvider : BaseInsertQueryProvider<IScriptWaypoint>, IDeleteQueryProvider<IScriptWaypoint>
{
    protected override object Convert(IScriptWaypoint wp) => new
    {
        entry = wp.PathId,
        pointid = wp.PointId,
        location_x = wp.X,
        location_y = wp.Y,
        location_z = wp.Z,
        waittime = wp.WaitTime,
        point_comment = wp.Comment,
    };

    public IQuery Delete(IScriptWaypoint wp) =>
        Queries.Table(TableName).Where(row => row.Column<uint>("entry") == wp.PathId).Delete();

    public override DatabaseTable TableName => DatabaseTable.WorldTable("script_waypoint");
}
