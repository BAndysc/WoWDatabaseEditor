using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class GuidExtractorProcessor : PacketProcessor<UniversalGuid?>
    {
        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketGossipSelect packet)
        {
            return packet.GossipUnit;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketGossipMessage packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketGossipHello packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketPlayObjectSound packet)
        {
            return packet.Source;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketPlaySound packet)
        {
            return packet.Source;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketEmote packet)
        {
            return packet.Sender;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketChat packet)
        {
            return packet.Sender;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketSpellGo packet)
        {
            return packet.Data?.Caster;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketSpellStart packet)
        {
            return packet.Data?.Caster;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketGossipClose packet)
        {
            return packet.GossipSource;
        }

        protected override UniversalGuid? Process(PacketBase packetBaseData, PacketAuraUpdate packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            return packet.Mover;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketPhaseShift packet)
        {
            return packet.Client;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            return packet.Target;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketSpellClick packet)
        {
            return packet.Target;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketPlayerLogin packet)
        {
            return packet.PlayerGuid;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketSetAnimKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketPlaySpellVisualKit packet)
        {
            return packet.Unit;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            return packet.QuestGiver;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketQuestGiverCompleteQuestRequest packet)
        {
            return packet.QuestGiver;
        }

        protected override UniversalGuid? Process(PacketBase basePacket, PacketUpdateObject packet)
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