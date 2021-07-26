using System.Collections.Generic;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureGameObjectNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketQueryCreatureResponse packet)
        {
            yield return ("Creature", packet.Name, packet.Entry);
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(PacketBase basePacket, PacketQueryGameObjectResponse packet)
        {
            yield return ("Gameobject", packet.Name, packet.Entry);
        }
    }
}