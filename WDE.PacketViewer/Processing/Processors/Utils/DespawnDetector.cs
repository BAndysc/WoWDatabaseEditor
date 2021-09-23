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
        protected override bool Process(PacketBase basePacket, PacketUpdateObject packet)
        {
            foreach (var create in packet.Created)
            {
                lastCreateTime[create.Guid] = (basePacket.Time.ToDateTime(), basePacket.Number);
            }

            foreach (var destroyed in packet.Destroyed)
            {
                if (lastCreateTime.TryGetValue(destroyed.Guid, out var spawn))
                {
                    var length = basePacket.Time.ToDateTime() - spawn.spawnTime;
                    var spawnPacketNumber = spawn.packetNumber;
                    spawnLength[(destroyed.Guid, spawnPacketNumber)] = length;
                    lastCreateTime.Remove(destroyed.Guid);
                }
            }
            return base.Process(basePacket, packet);
        }

        public TimeSpan? GetSpawnLength(UniversalGuid guid, int packetNumberSpawn)
        {
            if (spawnLength.TryGetValue((guid, packetNumberSpawn), out var length))
                return length;
            return null;
        }
    }
}