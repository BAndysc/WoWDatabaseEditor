using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Filtering
{
    public unsafe class IsPacketSpecificPlayerProcessor : PacketProcessor<bool>
    {
        private readonly UniversalGuid playerGuid;

        public IsPacketSpecificPlayerProcessor(UniversalGuid playerGuid)
        {
            this.playerGuid = playerGuid;
        }
        
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            return packet.Unit.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            return Unpack(packet.Data, x => x->Caster).Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
        {
            return Unpack(packet.Data, x => x->Caster).Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet)
        {
            return packet.Source.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet)
        {
            return packet.Source.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet)
        {
            return packet.Sender.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            return packet.Sender.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            return packet.Mover.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketOneShotAnimKit packet)
        {
            return packet.Unit.Equals(playerGuid);
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySpellVisualKit packet)
        {
            return packet.Unit.Equals(playerGuid);
        }
    }
}