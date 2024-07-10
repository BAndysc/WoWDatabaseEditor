using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public unsafe class GuidExtractorProcessor : PacketProcessor<UniversalGuid?>
    {
        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketGossipSelect packet)
        {
            return packet.GossipUnit;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketGossipMessage packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketGossipHello packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketPlayObjectSound packet)
        {
            return packet.Source;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketPlaySound packet)
        {
            return packet.Source;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketEmote packet)
        {
            return packet.Sender;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketChat packet)
        {
            return packet.Sender;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketSpellGo packet)
        {
            return Unpack(packet.Data, x => x->Caster);
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketSpellStart packet)
        {
            return Unpack(packet.Data, x => x->Caster);
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketGossipClose packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase packetBaseData, ref readonly PacketAuraUpdate packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketMonsterMove packet)
        {
            return packet.Mover;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketPhaseShift packet)
        {
            return packet.Client;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayMusic packet)
        {
            return packet.Target;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketSpellClick packet)
        {
            return packet.Target;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketPlayerLogin packet)
        {
            return packet.PlayerGuid;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketOneShotAnimKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketSetAnimKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketPlaySpellVisualKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverAcceptQuest packet)
        {
            return packet.QuestGiver;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketQuestGiverCompleteQuestRequest packet)
        {
            return packet.QuestGiver;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketClientUseGameObject packet)
        {
            return packet.GameObject;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketClientMove packet)
        {
            return packet.Mover;
        }

        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketClientQuestGiverChooseReward packet)
        {
            return packet.QuestGiver;
        }
        
        protected override UniversalGuid? Process(ref readonly PacketBase basePacket, ref readonly PacketUpdateObject packet)
        {
            if (packet.Created.Count + packet.Destroyed.Count + packet.Updated.Count + packet.OutOfRange.Count > 1)
                return null;

            if (packet.Created.Count == 1)
                return packet.Created[0].Guid;

            if (packet.Destroyed.Count == 1)
                return packet.Destroyed[0].Guid;

            if (packet.Updated.Count == 1)
                return packet.Updated[0].Guid;

            if (packet.OutOfRange.Count == 1)
                return packet.OutOfRange[0].Guid;

            return null;
        }
    }
}