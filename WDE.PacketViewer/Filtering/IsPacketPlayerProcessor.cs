using WDE.PacketViewer.Processing;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Filtering
{
    public class IsPacketPlayerProcessor : PacketProcessor<bool>
    {
        protected override bool Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketSpellGo packet)
        {
            return packet.Data.Caster.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketSpellStart packet)
        {
            return packet.Data.Caster.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            return packet.Source.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
        {
            return packet.Source.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
        {
            return packet.Sender.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketChat packet)
        {
            return packet.Sender.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            return packet.Mover.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySpellVisualKit packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }
    }
}