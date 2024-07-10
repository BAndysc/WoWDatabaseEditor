using System;
using System.Collections.Generic;
using System.Linq;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IUnitPositionFollower : IPacketProcessor<bool>
    {
        Vec3? GetPosition(UniversalGuid? guid, DateTime currentTime);
    }

    [AutoRegister]
    public unsafe class UnitPositionFollower : PacketProcessor<bool>, IUnitPositionFollower
    {
        private Dictionary<UniversalGuid, State> states = new();

        private class State
        {
            public bool IsDestroyed { get; set; } = true;
            public Vec3 CurrentPosition { get; set; } = new Vec3();
            public DateTime? StartMoveTime { get; set; }
            public uint LastMoveTime { get; set; }
            public Vec3 LastDestination { get; set; } = new();
        }

        private State GetState(UniversalGuid guid)
        {
            if (states.TryGetValue(guid, out var state))
                return state;
            return states[guid] = new(){ IsDestroyed = false };
        }

        public Vec3? GetPosition(UniversalGuid? guid, DateTime currentTime)
        {
            if (guid == null)
                return null;
            
            if (!states.TryGetValue(guid.Value, out var state) || state.IsDestroyed)
                return null;

            if (!state.StartMoveTime.HasValue)
                return state.CurrentPosition;

            if (currentTime >= state.StartMoveTime.Value + TimeSpan.FromMilliseconds(state.LastMoveTime))
                return state.CurrentPosition = state.LastDestination;

            var diff = (currentTime - state.StartMoveTime.Value).TotalMilliseconds;
            var progress = diff / state.LastMoveTime;

            var lerp = state.CurrentPosition.Lerp(state.LastDestination, (float)progress);
            return lerp;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketClientMove packet)
        {
            var state = GetState(packet.Mover);
            state.CurrentPosition = new Vec3() { X = packet.Position.X, Y = packet.Position.Y, Z = packet.Position.Z };
            return base.Process(in basePacket, in packet);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            var state = GetState(packet.Mover);
            state.CurrentPosition = packet.Position;
            if (packet.Points.Count > 0)
            {
                state.LastMoveTime = packet.MoveTime;
                state.StartMoveTime = basePacket.Time.ToDateTime();
                state.LastDestination = packet.Points[^1];
            }
            else
                state.StartMoveTime = null;
            
            return base.Process(in basePacket, in packet);
        }

        private List<UniversalGuid> toDestroy = new();

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            if (toDestroy.Count > 0)
            {
                // we are destroying object with delay, so that other packets could still use its position
                foreach (var destroyed in toDestroy)
                {
                    var state = GetState(destroyed);
                    state.IsDestroyed = true;
                }
                toDestroy.Clear();
            }
            
            foreach (ref readonly var create in packet.Created.AsSpan())
            {
                if (create.Movement == null && create.Stationary == null)
                    continue;

                var state = GetState(create.Guid);
                state.IsDestroyed = false;
                state.CurrentPosition = create.Movement != null ? create.Movement->Position : create.Stationary->Position;
            }
            
            foreach (ref readonly var destroyed in packet.Destroyed.AsSpan())
            {
                toDestroy.Add(destroyed.Guid);
            }

            foreach (ref readonly var oorange in packet.OutOfRange.AsSpan())
            {
                var state = GetState(oorange.Guid);
                state.IsDestroyed = true;
            }

            return true;
        }
    }
}