using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Filtering
{
    public unsafe class IsPacketPlayerProcessor : PacketProcessor<bool>
    {
        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketAuraUpdate packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellGo packet)
        {
            return packet.Data != null && packet.Data->Caster.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketSpellStart packet)
        {
            return packet.Data != null && packet.Data->Caster.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlayObjectSound packet)
        {
            return packet.Source.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySound packet)
        {
            return packet.Source.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketEmote packet)
        {
            return packet.Sender.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            return packet.Sender.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            return packet.Mover.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketOneShotAnimKit packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }

        protected override bool Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySpellVisualKit packet)
        {
            return packet.Unit.Type == UniversalHighGuid.Player;
        }
    }
}