using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Processing.Processors.Paths;

[AutoRegister]
[RequiresCore("TrinityCata", "TrinityWrath", "Azeroth", "TrintyMaster")]
public class SmartWaypointsSniffWaypointsExporter : ISniffWaypointsExporter
{
    public string Id => "smart_script_waypoints";
        
    public string Name => "Smart Script Waypoints (`waypoints`)";

    public override string ToString() => Name;

    public async Task Export(StringBuilder sb, int basePathNum, UniversalGuid guid, IWaypointProcessor.UnitMovementState state, float randomness)
    {
        var q = Queries.BeginTransaction(DataDatabaseType.World);
        List<object> toInsert = new();
            
        uint pathEntry = guid.Entry;

        if (basePathNum > 0)
            pathEntry = (uint)(guid.Entry * 100 + (basePathNum - 1));

        foreach (var path in state.Paths)
        {
            bool outOfRange = pathEntry > guid.Entry * 100 + 99;

            if (outOfRange)
                q.StartBlockComment("Those paths are out of range, because a single entry can have up to 100 paths");
            
            int pointid = 1;
            q.Table(DatabaseTable.WorldTable("waypoints")).Where(row => row.Column<uint>("entry") == pathEntry).Delete();
            toInsert.Clear();
            foreach (var segment in path.Segments)
            {
                for (var index = 0; index < segment.Waypoints.Count; index++)
                {
                    var isFirst = index == 0;
                    var isLast = index == segment.Waypoints.Count - 1;
                        
                    var waypoint = segment.Waypoints[index];
                    toInsert.Add(new
                    {
                        entry = pathEntry,
                        pointid = pointid++,
                        position_x = waypoint.X,
                        position_y = waypoint.Y,
                        position_z = waypoint.Z,
                        orientation = isLast ? segment.FinalOrientation : null,
                        delay = isFirst ? (int)(segment.Wait?.TotalMilliseconds ?? 0) : 0
                    });
                }
            }

            if (toInsert.Count > 0)
                q.Table(DatabaseTable.WorldTable("waypoints")).BulkInsert(toInsert);

            if (outOfRange)
                q.EndBlockComment();
            
            if (pathEntry == guid.Entry)
                pathEntry = guid.Entry * 100;
            else
                pathEntry++;

            q.BlankLine();
        }

        sb.AppendLine(q.Close().QueryString);
    }
}