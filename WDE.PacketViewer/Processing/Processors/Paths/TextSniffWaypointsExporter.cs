using System;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Processing.Processors.Paths;

[AutoRegister]
public class TextSniffWaypointsExporter : ISniffWaypointsExporter
{
    public string Id => "text";
        
    public string Name => "Text";

    public override string ToString() => Name;

    public async Task Export(StringBuilder sb, int basePathNum, UniversalGuid guid, IWaypointProcessor.UnitMovementState state, float randomness)
    {
        sb.AppendLine("Creature " + guid.ToWowParserString() + $" (entry: {guid.Entry})  randomness: {(randomness) * 100:0.00}%");
        int pathId = 0;
        int segmentId = 0;
        DateTime? prevPathFinishedTime = null;
        foreach (var path in state.Paths)
        {
            if (path.IsFirstPathAfterSpawn)
                sb.AppendLine("    (object created, so it's his first path ever)");
            if (!path.IsContinuationAfterPause)
                sb.AppendLine("    (despawn) ");

            if (prevPathFinishedTime != null && path.IsContinuationAfterPause)
            {
                var wait = (path.PathStartTime - prevPathFinishedTime.Value);
                if (wait.TotalSeconds < 10)
                    sb.AppendLine($"    (after {(ulong)wait.TotalMilliseconds} ms)");
                else
                    sb.AppendLine($"    (after {wait.ToNiceString()} [{(ulong)wait.TotalMilliseconds} ms])");
            }

            sb.AppendLine("* Path " + pathId++);
            segmentId = 0;

            foreach (var segment in path.Segments)
            {
                if (segment.Wait.HasValue)
                    sb.AppendLine($"    wait {(ulong)segment.Wait.Value.TotalMilliseconds} ms");

                sb.AppendLine("   Segment " + segmentId++);
                if (segment.JumpGravity.HasValue)
                {
                    var p = segment.Waypoints[^1];
                    sb.AppendLine(
                        $"    jump to ({p.X}, {p.Y}, {p.Z}) gravity: {segment.JumpGravity.Value} move time: {segment.MoveTime}");
                }
                else
                {
                    foreach (var p in segment.Waypoints)
                    {
                        sb.AppendLine($"    ({p.X}, {p.Y}, {p.Z})");
                    }
                }
            }

            if (path.DestroysAfterPath)
                sb.AppendLine("    (object destroyed, so this is the last point for sure)");
            sb.AppendLine("");

            prevPathFinishedTime = path.PathStartTime + TimeSpan.FromMilliseconds(path.TotalMoveTime);
        }
    }
}