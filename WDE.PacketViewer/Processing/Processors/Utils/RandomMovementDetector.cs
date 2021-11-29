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
         * Returns a number from 0 to 1, how much the detector thinks the guid unit is using random movement
         * (1 - almost certain)
         * (0 - no chance for random movement)
         */
        float RandomMovementPacketRatio(UniversalGuid guid);
    }
}