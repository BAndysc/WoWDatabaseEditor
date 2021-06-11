using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class EntryExtractorProcessor : PacketProcessor<uint>
    {
        protected override uint Process(PacketBase packetBaseData, PacketGossipSelect packet)
        {
            return packet.GossipUnit.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketGossipMessage packet)
        {
            return packet.GossipSource.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketGossipHello packet)
        {
            return packet.GossipSource.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketPlayObjectSound packet)
        {
            return packet.Source?.Entry ?? 0;
        }

        protected override uint Process(PacketBase packetBaseData, PacketPlaySound packet)
        {
            return packet.Source?.Entry ?? 0;
        }

        protected override uint Process(PacketBase packetBaseData, PacketEmote packet)
        {
            return packet.Sender.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketChat packet)
        {
            return packet.Sender.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketSpellGo packet)
        {
            return packet.Data.Caster.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketSpellStart packet)
        {
            return packet.Data.Caster.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketGossipClose packet)
        {
            return packet.GossipSource.Entry;
        }

        protected override uint Process(PacketBase packetBaseData, PacketAuraUpdate packet)
        {
            return packet.Unit.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketMonsterMove packet)
        {
            return packet.Mover.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketPhaseShift packet)
        {
            return packet.Client.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketPlayMusic packet)
        {
            return packet.Target.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketQueryCreatureResponse packet)
        {
            return packet.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketSpellClick packet)
        {
            return packet.Target.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketPlayerLogin packet)
        {
            return packet.PlayerGuid.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketOneShotAnimKit packet)
        {
            return packet.Unit.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketSetAnimKit packet)
        {
            return packet.Unit.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketPlaySpellVisualKit packet)
        {
            return packet.Unit.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketQuestGiverAcceptQuest packet)
        {
            return packet.QuestGiver.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketQuestGiverCompleteQuestRequest packet)
        {
            return packet.QuestGiver.Entry;
        }

        protected override uint Process(PacketBase basePacket, PacketQuestGiverRequestItems packet)
        {
            return packet.QuestGiverEntry;
        }
    }
}