using System.Collections.Generic;
using WDE.PacketViewer.Utils;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureGameObjectNameProcessor : PacketProcessor<IEnumerable<(string prefix, string name, uint entry)>>
    {
        protected override IEnumerable<(string prefix, string name, uint entry)> Process(ref readonly PacketBase basePacket, ref readonly PacketQueryCreatureResponse packet)
        {
            return ("Creature", packet.Name.ToString() ?? "", packet.Entry).ToSingletonList();
        }

        protected override IEnumerable<(string prefix, string name, uint entry)> Process(ref readonly PacketBase basePacket, ref readonly PacketQueryGameObjectResponse packet)
        {
            return ("Gameobject", packet.Name.ToString() ?? "", packet.Entry).ToSingletonList();
        }
    }
}