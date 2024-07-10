using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ProtoZeroSharp;
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
            public float? JumpGravity { get; set; }
            public TimeSpan? Wait { get; set; }

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
    public unsafe class WaypointsProcessor : PacketProcessor<bool>, IWaypointProcessor
    {
        private Dictionary<int, int> PacketToOriginalSplinePacket { get; } = new();
        public Dictionary<UniversalGuid, IWaypointProcessor.UnitMovementState> State { get; } = new();
        private HashSet<UniversalGuid> notRandom = new();
        private Dictionary<UniversalGuid, float> cachedRatios = new();

        private IWaypointProcessor.UnitMovementState Get(UniversalGuid guid)
        {
            if (State.TryGetValue(guid, out var unit))
                return unit;
            
            return State[guid] = new IWaypointProcessor.UnitMovementState();
        }
        
        private void ProcessUpdateMovement(PacketBase basePacket, IWaypointProcessor.UnitMovementState state, ref MovementUpdate movement)
        {
            if (state.InCombat)
                return;

            if (movement.SplineData == null || movement.SplineData->MoveData == null)
                return;

            CheckIfNotRandom(state, movement.Mover, movement.SplineData->MoveData->Flags, movement.SplineData->MoveData->Jump);

            if (movement.SplineData->MoveData->Points.Count == 0)
                return;
            
            state.LastDestroyed = 0;
            
            state.Paths.Add(new IWaypointProcessor.Path(){FirstPacketNumber = basePacket.Number,
                IsContinuationAfterPause = false,
                PathStartTime = basePacket.Time.ToDateTime()
            });
            state.Paths[^1].IsFirstPathAfterSpawn = state.Paths.Count == 1 && state.JustSpawned;
            state.LastSegment = null;

            float distance = movement.SplineData->MoveData->Points[0].TotalDistance(movement.SplineData->MoveData->Points);

            float? finalOrientation = movement.SplineData->MoveData->FacingCase == MovementSplineMoveData.FacingOneofCase.LookOrientation
                ? movement.SplineData->MoveData->LookOrientation
                : null;
            state.Paths[^1].Segments.Add(new IWaypointProcessor.Segment(movement.MoveTime, distance, movement.SplineData->MoveData->Points[0], finalOrientation));
            state.LastSegment = state.Paths[^1].Segments[^1];

            if (movement.SplineData->MoveData->Points.Count >= 1)
            {
                state.Paths[^1].Segments[^1].Waypoints.AddRange(movement.SplineData->MoveData->Points);
            }

            if ((movement.SplineData->MoveData->Flags & UniversalSplineFlag.Parabolic) != 0 && movement.SplineData->MoveData->Jump != null)
            {
                state.Paths[^1].Segments[^1].JumpGravity = movement.SplineData->MoveData->Jump->Gravity;
            }

            state.LastMovement = basePacket.Time.ToDateTime();
            state.LastMoveTime = movement.MoveTime;
            state.LastMovementNumber = basePacket.Number;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            var state = Get(packet.Mover);
            if (state.InCombat)
                return true;

            CheckIfNotRandom(state, packet.Mover, packet.Flags, packet.Jump);
            
            if (packet.PackedPoints.Count + packet.Points.Count == 0 || packet.Points.Count == 0)
                return true;

            //if (packet.PackedPoints.Count == 0 && packet.Points.Count == 1 &&
            //    packet.FacingCase != PacketMonsterMove.FacingOneofCase.None)
            //{
            //    Console.WriteLine("Post in combat fix");
            //    return true;
            //}

            var timeSinceLastMovement = basePacket.Time.ToDateTime() - state.LastMovement;
            // if > 0, then it has finished, if < 0 it hasn't finished yet
            var timeAfterLastMovementShallFinish = timeSinceLastMovement.TotalMilliseconds - state.LastMoveTime;

            // if wait is longer than arbitrary chosen pause time, then we treat it as separate paths
            const int PAUSE_MIN_TIME = 800;
            
            TimeSpan? waitAfterTheLast = timeAfterLastMovementShallFinish > 0 && timeAfterLastMovementShallFinish < PAUSE_MIN_TIME ? TimeSpan.FromMilliseconds(timeAfterLastMovementShallFinish) : null;
            bool resumeAfterPause = timeAfterLastMovementShallFinish >= PAUSE_MIN_TIME;
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
            else if (state.Paths.Count > 0 && !resumeAfterPause && !waitAfterTheLast.HasValue)
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
            state.LastSegment.Wait = waitAfterTheLast;

            if (packet.PackedPoints.Length > 0)
            {
                foreach (ref readonly var point in packet.PackedPoints.AsSpan())
                { 
                    state.Paths[^1].Segments[^1].Waypoints.Add(point);
                }

                Debug.Assert(packet.Points.Length == 1);
                state.Paths[^1].Segments[^1].Waypoints.Add(packet.Points[0]);
            }
            else if (packet.Points.Length >= 1)
            {
                state.Paths[^1].Segments[^1].Waypoints.AddRange(packet.Points);
            }

            if ((packet.Flags & UniversalSplineFlag.Parabolic) != 0 && packet.Jump != null)
            {
                state.Paths[^1].Segments[^1].JumpGravity = packet.Jump->Gravity;
            }

            state.LastMovement = basePacket.Time.ToDateTime();
            state.LastMoveTime = packet.MoveTime;
            state.LastMovementNumber = basePacket.Number;
            
            return true;
        }

        private void CheckIfNotRandom(IWaypointProcessor.UnitMovementState state, UniversalGuid mover, UniversalSplineFlag flags, SplineJump* jump)
        {
            if ((flags & UniversalSplineFlag.Parabolic) != 0  ||
                (jump != null && jump->Gravity > 0) ||
                (flags & UniversalSplineFlag.TransportEnter) != 0)
            {
                notRandom.Add(mover);
            }
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            for (int i = 0; i < packet.Created.Length; ++i)
            {
                ref var create = ref packet.Created[i];

                var state = Get(create.Guid);
                state.JustSpawned = create.CreateType == CreateObjectType.Spawn;
                if (create.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
                    state.InCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;

                if (create.Movement != null && create.Movement->SplineData != null && create.Movement->SplineData->MoveData != null)
                    ProcessUpdateMovement(basePacket, state, ref *create.Movement);
            }
            
            for (int i = 0; i < packet.Updated.Length; ++i)
            {
                ref var update = ref packet.Updated[i];
                if (update.Values.TryGetInt("UNIT_FIELD_FLAGS", out var flags))
                    Get(update.Guid).InCombat = (flags & (uint)GameDefines.UnitFlags.InCombat) == (uint)GameDefines.UnitFlags.InCombat;
            }

            for (int i = 0; i < packet.Destroyed.Length; ++i)
            {
                ref var destroyed = ref packet.Destroyed[i];
                var state = Get(destroyed.Guid);
                if (state.Paths.Count > 0)
                    state.Paths[^1].DestroysAfterPath = true;
                state.LastDestroyed = basePacket.Number;
            }

            for (int i = 0; i < packet.OutOfRange.Length; ++i)
            {
                ref var outOfRange = ref packet.OutOfRange[i];

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

            float CalculateRatio()
            {
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
                            var thisForward = Vector2.Normalize(thisPoint - prevPoint);
                            var dotProduct = Vector2.Dot(prevForward, thisForward);

                            if (float.IsNaN(dotProduct))
                                continue; // it can happen if two points have the same x and y, i.e. the creature is flying

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

            if (!cachedRatios.TryGetValue(guid, out var ratio))
                cachedRatios[guid] = ratio = CalculateRatio();
            return ratio;
        }
    }
}