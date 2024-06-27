using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class CreatureTemplateProcessor : PacketProcessor<bool>
    {
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketQueryCreatureResponse packet)
        {
            
            return false;
        }
    }
}