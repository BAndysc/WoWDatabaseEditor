using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TheMaths;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IWaypointProcessor : IPacketProcessor<bool>, IRandomMovementDetector
    {
        Dictionary<UniversalGuid, UnitMovementState> State { get; }
        int? GetOriginalSpline(int packetNumber);
        
        public class UnitMovementState
        {
            public bool JustSpawned { get; set; }
            public bool InCombat { get; set; }
            public DateTime LastMovement { get; set; }
            public uint LastMoveTime { get; set; }
            public int LastDestroyed { get; set; }
            public int LastMovementNumber { get; set; }

            public List<Path> Paths { get; } = new();
            public Segment? LastSegment { get; set; }
        }

        public class Path
        {
            public DateTime PathStartTime;
            public int FirstPacketNumber;
            public List<Segment> Segments { get; } = new();

            public bool IsContinuationAfterPause;
            public bool DestroysAfterPath;
            public bool IsFirstPathAfterSpawn;
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
            public uint MoveTime { get; set; }
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

    [AutoRegister]
    public class WaypointsProcessor : PacketProcessor<bool>, IWaypointProcessor
    {
        private Dictionary<int, int> PacketToOriginalSplinePacket { get; } = new();
        public Dictionary<UniversalGuid, IWaypointProcessor.UnitMovementState> State { get; } = new();
        private HashSet<UniversalGuid> notRandom = new();

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

            if ((packet.Flags & UniversalSplineFlag.Parabolic) != 0  ||
                (packet.Jump != null && packet.Jump.Gravity > 0) ||
                (packet.Flags & UniversalSplineFlag.TransportEnter) != 0)
            {
                notRandom.Add(packet.Mover);
            }
            
            if (packet.PackedPoints.Count + packet.Points.Count == 0 || packet.Points.Count == 0)
                return true;

            //if (packet.PackedPoints.Count == 0 && packet.Points.Count == 1 &&
            //    packet.FacingCase != PacketMonsterMove.FacingOneofCase.None)
            //{
            //    Console.WriteLine("Post in combat fix");
            //    return true;
            //}

            var timeSinceLastMovement = basePacket.Time.ToDateTime() - state.LastMovement;

            bool resumeAfterPause = timeSinceLastMovement.TotalMilliseconds >= state.LastMoveTime;
            bool firstMovementAfterSpawn = state.LastDestroyed > 0;
            state.LastDestroyed = 0;
            
            if (state.LastMoveTime == 0 || resumeAfterPause)
            {
                state.Paths.Add(new IWaypointProcessor.Path(){FirstPacketNumber = basePacket.Number,
                    IsContinuationAfterPause = !firstMovementAfterSpawn && resumeAfterPause,
                    PathStartTime = basePacket.Time.ToDateTime()
                });
                state.Paths[^1].IsFirstPathAfterSpawn = state.Paths.Count == 1 && state.JustSpawned;
                state.LastSegment = null;
            }
            else if (state.Paths.Count > 0 && !resumeAfterPause)
            {
                Debug.Assert(state.LastSegment != null);
                PacketToOriginalSplinePacket[basePacket.Number] = state.Paths[^1].FirstPacketNumber;

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
                    state.Paths[^1].Segments[^1].MoveTime = (uint)(howManyFinished * state.Paths[^1].Segments[^1].MoveTime);
                    for (int i = 0; i < toRemove; ++i)
                        state.Paths[^1].Segments[^1].Waypoints.RemoveAt(state.Paths[^1].Segments[^1].Waypoints.Count - 1);
                    state.Paths[^1].Segments[^1].Waypoints.Add(packet.Position);
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
            else if (packet.Points.Count >= 1)
            {
                state.Paths[^1].Segments[^1].Waypoints.AddRange(packet.Points);
            }

            state.LastMovement = basePacket.Time.ToDateTime();
            state.LastMoveTime = packet.MoveTime;
            state.LastMovementNumber = basePacket.Number;
            
            return true;
        }

        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var create in packet.Created)
            {
                Get(create.Guid).JustSpawned = create.CreateType == CreateObjectType.Spawn;
                if (create.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
                    Get(create.Guid).InCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;
            }
            
            foreach (var update in packet.Updated)
            {
                if (update.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
                    Get(update.Guid).InCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;
            }

            foreach (var destroyed in packet.Destroyed)
            {
                var state = Get(destroyed.Guid);
                if (state.Paths.Count > 0)
                    state.Paths[^1].DestroysAfterPath = true;
                state.LastDestroyed = basePacket.Number;
            }

            foreach (var outOfRange in packet.OutOfRange)
            {
                Get(outOfRange.Guid).LastDestroyed = basePacket.Number;
            }

            return true;
        }
        
        public int? GetOriginalSpline(int packetNumber)
        {
            if (PacketToOriginalSplinePacket.TryGetValue(packetNumber, out var original))
                return original;
            return null;
        }

        public float RandomMovementPacketRatio(UniversalGuid guid)
        {
            if (notRandom.Contains(guid))
                return 0;
            
            if (!State.TryGetValue(guid, out var state))
                return -1;

            Vector2 prevPoint = Vector2.Zero;
            Vector2 thisPoint = Vector2.Zero;
            Vector2 prevForward = Vector2.Zero;
            float anglesSum = 0;
            int anglesCount = 0;

            int i = 0;
            foreach (var path in state.Paths)
            {
                bool pathAfterDespawn = !path.IsContinuationAfterPause;
                if (path.IsContinuationAfterPause)
                {
                    anglesSum += -1; // each pause means higher chance of random movement
                    anglesCount += 1;
                }
                foreach (var s in path.Segments)
                {
                    for (var index = 0; index < s.Waypoints.Count; index++)
                    {
                        var w = s.Waypoints[index];
                        thisPoint = new Vector2(w.X, w.Y);
                        var thisForward = (thisPoint - prevPoint).Normalized;
                        var dotProduct = Vector2.Dot(prevForward, thisForward);

                        if (index != s.Waypoints.Count - 1 && // if this is the last point, then we want to check its angle
                            Math.Abs(dotProduct) > 0.90f) // going almost straight, let's ignore the point, as it is most likely a Z correction waypoint
                        {
                            continue;
                        }

                        if (i >= 2 && !pathAfterDespawn)
                        {
                            anglesSum += dotProduct;
                            anglesCount += 1;
                        }

                        prevPoint = thisPoint;
                        prevForward = thisForward;
                        i++;
                        pathAfterDespawn = false;
                    }
                }   
            }

            if (anglesCount == 0)
                return 0.5f;
            return 1 - ((anglesSum / anglesCount) / 2 + 0.5f);
        }
    }
}