using System;
using System.Collections.Generic;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    [UniqueProvider]
    public interface IRandomMovementDetector : IPacketProcessor<bool>
    {
        /**
         * Returns true if the detector thinks this packet is random move packet
         */
        bool IsRandomMovementPacket(PacketBase packet);
        
        /**
         * Returns a number from 0 to 1, how much the detector thinks the guid unit is using random movement
         * (1 - almost certain)
         * (0 - no chance for random movement)
         */
        float RandomMovementPacketRatio(UniversalGuid guid);
    }

    [AutoRegister]
    public class RandomMovementDetector : PacketProcessor<bool>, IRandomMovementDetector
    {
        private class State
        {
            public int totalMovePackets;
            public int totalRandomLookalike;
            public int totalNotRandomMovePackets;
            public (PacketBase, PacketMonsterMove)? lastMovement;
        }

        private Dictionary<UniversalGuid, State> states = new();

        private HashSet<int> lookingRandomly = new();

        private State Get(UniversalGuid guid)
        {
            if (states.TryGetValue(guid, out var state))
                return state;
            return states[guid] = new();
        }
        
        public bool IsRandomMovementPacket(PacketBase packet)
        {
            return lookingRandomly.Contains(packet.Number);
        }

        public float RandomMovementPacketRatio(UniversalGuid guid)
        {
            if (!states.TryGetValue(guid, out var state))
                return 0;
            if (state.totalNotRandomMovePackets > 0)
                return 0;
            return 1.0f * state.totalRandomLookalike / state.totalMovePackets;
        }
        
        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            var state = Get(packet.Mover);
            state.totalMovePackets++;
            if (state.lastMovement == null)
            {
                state.lastMovement = (basePacket, packet);
                return true;
            }

            var lastMovementPacket = state.lastMovement.Value.Item1;
            var lastMovement = state.lastMovement.Value.Item2;

            if (lastMovement.Points.Count == 1 && 
                lastMovement.PackedPoints.Count <= 2 &&
                lastMovement.Flags.HasFlag(UniversalSplineFlag.SmoothGroundPath) &&
                lastMovement.FacingCase == PacketMonsterMove.FacingOneofCase.None)
            {
                lookingRandomly.Add(lastMovementPacket.Number);
                state.totalRandomLookalike++;
            }
            else if (lastMovement.Points.Count > 1)
                state.totalNotRandomMovePackets++;
            
            state.lastMovement = (basePacket, packet);
            return base.Process(basePacket, packet);
        }
    }
}