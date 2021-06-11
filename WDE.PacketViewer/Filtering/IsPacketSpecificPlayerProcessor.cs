using WDE.PacketViewer.Processing;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Filtering
{
    public class IsPacketSpecificPlayerProcessor : PacketProcessor<bool>
    {
        private readonly UniversalGuid playerGuid;

        public IsPacketSpecificPlayerProcessor(UniversalGuid playerGuid)
        {
            this.playerGuid = playerGuid;
        }
        
        protected override bool Process(PacketBase basePacket, PacketAuraUpdate packet)
        {
            return packet.Unit.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellGo packet)
        {
            return packet.Data.Caster.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketSpellStart packet)
        {
            return packet.Data.Caster.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketPlayObjectSound packet)
        {
            return packet.Source.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySound packet)
        {
            return packet.Source.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketEmote packet)
        {
            return packet.Sender.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketChat packet)
        {
            return packet.Sender.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            return packet.Mover.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            return packet.Unit.Equals(playerGuid);
        }

        protected override bool Process(PacketBase basePacket, PacketPlaySpellVisualKit packet)
        {
            return packet.Unit.Equals(playerGuid);
        }
    }
}