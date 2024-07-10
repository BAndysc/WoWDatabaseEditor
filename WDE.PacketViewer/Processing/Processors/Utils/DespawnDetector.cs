using System;
using System.Collections.Generic;
using WDE.Module.Attributes;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public interface IDespawnDetector : IPacketProcessor<bool>
    {
        public TimeSpan? GetSpawnLength(UniversalGuid guid, int packetNumberSpawn);
    }
    
    [AutoRegister]
    public class DespawnDetector : PacketProcessor<bool>, IDespawnDetector
    {
        private Dictionary<UniversalGuid, (DateTime spawnTime, int packetNumber)> lastCreateTime = new();
        private Dictionary<(UniversalGuid, int), TimeSpan> spawnLength = new();
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            foreach (ref readonly var create in packet.Created.AsSpan())
            {
                lastCreateTime[create.Guid] = (basePacket.Time.ToDateTime(), basePacket.Number);
            }

            foreach (ref readonly var destroyed in packet.Destroyed.AsSpan())
            {
                if (lastCreateTime.TryGetValue(destroyed.Guid, out var spawn))
                {
                    var length = basePacket.Time.ToDateTime() - spawn.spawnTime;
                    var spawnPacketNumber = spawn.packetNumber;
                    spawnLength[(destroyed.Guid, spawnPacketNumber)] = length;
                    lastCreateTime.Remove(destroyed.Guid);
                }
            }
            return base.Process(in basePacket, in packet);
        }

        public TimeSpan? GetSpawnLength(UniversalGuid guid, int packetNumberSpawn)
        {
            if (spawnLength.TryGetValue((guid, packetNumberSpawn), out var length))
                return length;
            return null;
        }
    }
}