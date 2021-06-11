using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureTemplateProcessor : PacketProcessor<bool>
    {
        protected override bool Process(PacketBase basePacket, PacketQueryCreatureResponse packet)
        {
            
            return false;
        }
    }
}