using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Utils;
using WDE.PacketViewer.Utils;
using WDE.SqlQueryGenerator;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors.Utils
{
    public class WaypointsToTextProcessor : IPacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IWaypointProcessor waypointProcessor;
        private readonly IParsingSettings parsingSettings;

        public WaypointsToTextProcessor(IWaypointProcessor waypointProcessor, IParsingSettings parsingSettings)
        {
            this.waypointProcessor = waypointProcessor;
            this.parsingSettings = parsingSettings;
        }

        public bool Process(PacketHolder packet) => waypointProcessor.Process(packet);

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            Dictionary<UniversalGuid, float> randomnessMap = new();
            foreach (var unit in waypointProcessor.State)
                randomnessMap[unit.Key] = waypointProcessor.RandomMovementPacketRatio(unit.Key);

            foreach (var unit in waypointProcessor
                         .State
                         .OrderBy(pair => randomnessMap[pair.Key]))
            {
                if (unit.Key.Type == UniversalHighGuid.Player)
                    continue;
                
                if (unit.Value.Paths.Count == 0)
                    continue;

                var randomness = waypointProcessor.RandomMovementPacketRatio(unit.Key);
                if (parsingSettings.WaypointsDumpType == WaypointsDumpType.Text)
                    GenerateTextOutput(sb, unit.Key, unit.Value, randomness);
                else if (parsingSettings.WaypointsDumpType == WaypointsDumpType.SmartWaypoints)
                    GenerateSmartWaypointsOutput(sb, unit.Key, unit.Value, randomness);
                else
                    throw new ArgumentOutOfRangeException("parsingSettings.WaypointsDumpType out of range");
            }

            return sb.ToString();
        }

        private void GenerateSmartWaypointsOutput(StringBuilder sb, UniversalGuid guid, IWaypointProcessor.UnitMovementState state, float randomness)
        {
            var q = Queries.BeginTransaction();
            List<object> toInsert = new();
            
            uint pathEntry = guid.Entry;

            foreach (var path in state.Paths)
            {
                int pointid = 1;
                q.Table("waypoints").Where(row => row.Column<uint>("entry") == pathEntry).Delete();
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
                    q.Table("waypoints").BulkInsert(toInsert);

                if (pathEntry == guid.Entry)
                    pathEntry = guid.Entry * 100;
                else
                    pathEntry++;

                q.BlankLine();
            }

            sb.AppendLine(q.Close().QueryString);
        }

        private static void GenerateTextOutput(StringBuilder sb, UniversalGuid guid, IWaypointProcessor.UnitMovementState state, float randomness)
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
}