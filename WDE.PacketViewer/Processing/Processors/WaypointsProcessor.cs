using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IWaypointProcessor : IPacketProcessor<bool>
    {
        Dictionary<UniversalGuid, UnitMovementState> State { get; }
        
        public class UnitMovementState
        {
            public bool InCombat { get; set; }
            public DateTime LastMovement { get; set; }
            public uint LastMoveTime { get; set; }

            public List<Path> Paths { get; } = new();
            public Segment? LastSegment { get; set; }
        }

        public class Path
        {
            public int FirstPacketNumber;
            public List<Segment> Segments { get; } = new();

            public uint TotalMoveTime => (uint)Segments.Sum(s => s.MoveTime);
        }

        public class Segment
        {
            public Segment(uint moveTime, float originalDistance, Vec3 initialNpcPosition, float? finalOrientation)
            {
                MoveTime = moveTime;
                OriginalDistance = originalDistance;
                InitialNpcPosition = initialNpcPosition;
                FinalOrientation = finalOrientation;
            }

            public float? FinalOrientation { get; }
            public uint MoveTime { get; }
            public Vec3 InitialNpcPosition { get; }
            public List<Vec3> Waypoints { get; } = new();
            public float OriginalDistance { get; }
            
            public float FinalLength()
            {
                float dist = 0;
                Vec3 prev = InitialNpcPosition;
                for (int i = 0; i < Waypoints.Count; ++i)
                {
                    var cur = Waypoints[i];
                    dist += prev.Distance3D(cur);
                    prev = cur;
                }

                return dist;
            }
        }
    }

    public class WaypointsToTextProcessor : IPacketProcessor<bool>, IPacketTextDumper
    {
        private readonly IWaypointProcessor waypointProcessor;

        public WaypointsToTextProcessor(IWaypointProcessor waypointProcessor)
        {
            this.waypointProcessor = waypointProcessor;
        }

        public bool Process(PacketHolder packet) => waypointProcessor.Process(packet);

        public async Task<string> Generate()
        {
            StringBuilder sb = new();
            foreach (var unit in waypointProcessor.State)
            {
                if (unit.Value.Paths.Count == 0)
                    continue;

                sb.AppendLine("Creature " + unit.Key.ToHexString() + $" (entry: {unit.Key.Entry})");
                int i = 0;
                foreach (var path in unit.Value.Paths)
                {
                    sb.AppendLine("  Path " + i++);
                    int j = 0;
                    foreach (var segment in path.Segments)
                    {
                        sb.AppendLine("  Segment " + j++);
                        foreach (var p in segment.Waypoints)
                        {
                            sb.AppendLine($"    ({p.X}, {p.Y}, {p.Z})");
                        }
                    }

                    sb.AppendLine("");
                }
            }

            return sb.ToString();
        }
    }

    [AutoRegister]
    public class WaypointsProcessor : PacketProcessor<bool>, IWaypointProcessor
    {
        public Dictionary<UniversalGuid, IWaypointProcessor.UnitMovementState> State { get; } = new();

        private IWaypointProcessor.UnitMovementState Get(UniversalGuid guid)
        {
            if (State.TryGetValue(guid, out var unit))
                return unit;
            
            return State[guid] = new IWaypointProcessor.UnitMovementState();
        }
        
        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            var state = Get(packet.Mover);
            if (state.InCombat)
                return true;

            if (packet.PackedPoints.Count + packet.Points.Count == 0)
                return true;

            //if (packet.PackedPoints.Count == 0 && packet.Points.Count == 1 &&
            //    packet.FacingCase != PacketMonsterMove.FacingOneofCase.None)
            //{
            //    Console.WriteLine("Post in combat fix");
            //    return true;
            //}

            var timeSinceLastMovement = basePacket.Time.ToDateTime() - state.LastMovement;

            if (state.LastMoveTime == 0 || timeSinceLastMovement.TotalMilliseconds > state.LastMoveTime)
            {
                state.Paths.Add(new IWaypointProcessor.Path(){FirstPacketNumber = basePacket.Number});
                state.LastSegment = null;
            }
            else if (state.Paths.Count > 0 && timeSinceLastMovement.TotalMilliseconds < state.LastMoveTime)
            {
                Debug.Assert(state.LastSegment != null);

                var howManyFinished = timeSinceLastMovement.TotalMilliseconds / state.LastMoveTime;

                var totalDist = state.LastSegment.InitialNpcPosition.TotalDistance(state.LastSegment.Waypoints);
                var allowedDist = totalDist * howManyFinished;

                int rejectAt = -1;
                var tempDist = 0.0;
                var prev = state.LastSegment.InitialNpcPosition;
                for (var index = 0; index < state.LastSegment.Waypoints.Count; index++)
                {
                    var cur = state.LastSegment.Waypoints[index];
                    tempDist += prev.Distance2D(cur);
                    if (tempDist > allowedDist)
                    {
                        rejectAt = index;
                        break;
                    }
                    prev = cur;
                }

                if (rejectAt > -1)
                {
                    int toRemove = (state.LastSegment.Waypoints.Count - rejectAt);
                    for (int i = 0; i < toRemove; ++i)
                        state.Paths[^1].Segments[^1].Waypoints.RemoveAt(state.Paths[^1].Segments[^1].Waypoints.Count - 1);
                }

                if (state.Paths[^1].Segments[^1].Waypoints.Count == 0)
                {
                    state.Paths[^1].Segments.RemoveAt(state.Paths[^1].Segments.Count - 1);
                }
            }

            float distance = 0;
            if (packet.PackedPoints.Count > 0)
                distance = packet.Position.TotalDistance(packet.PackedPoints, packet.Points[0]);
            else
                distance = packet.Position.TotalDistance(packet.Points);

            float? finalOrientation = packet.FacingCase == PacketMonsterMove.FacingOneofCase.LookOrientation
                ? packet.LookOrientation
                : null;
            state.Paths[^1].Segments.Add(new IWaypointProcessor.Segment(packet.MoveTime, distance, packet.Position, finalOrientation));
            state.LastSegment = state.Paths[^1].Segments[^1];            

            if (packet.PackedPoints.Count > 0)
            {
                foreach (var point in packet.PackedPoints)
                { 
                    state.Paths[^1].Segments[^1].Waypoints.Add(point);
                }

                Debug.Assert(packet.Points.Count == 1);
                state.Paths[^1].Segments[^1].Waypoints.Add(packet.Points[0]);
            }
            else if (packet.Points.Count == 1)
            {
                state.Paths[^1].Segments[^1].Waypoints.Add(packet.Points[0]);
            }

            state.LastMovement = basePacket.Time.ToDateTime();
            state.LastMoveTime = packet.MoveTime;
            
            return true;
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var create in packet.Created)
            {
                if (create.Values.Ints.TryGetValue("UNIT_FIELD_FLAGS", out var flags))
                    Get(create.Guid).InCombat = (flags & (uint)GameDefines.UnitFlags.UnitFlagInCombat) == (uint)GameDefines.UnitFlags.UnitFlagInCombat;
            }
            
            foreach (var update in packet.Updated)
            {
                if (update.Values.Ints.TryGetValue("UNIT_FIELD_FLAGS", out var flags))
                    Get(update.Guid).InCombat = (flags & (uint)GameDefines.UnitFlags.UnitFlagInCombat) == (uint)GameDefines.UnitFlags.UnitFlagInCombat;
            }

            return true;
        }
    }
}