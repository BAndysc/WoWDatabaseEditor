using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.Processing.Processors
{
    public class EntryExtractorProcessor : IPacketProcessor<uint>
    {
        private static IPacketProcessor<UniversalGuid?> guidExtractor = new GuidExtractorProcessor();
        
        public uint Process(ref readonly PacketHolder packet)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryCreatureResponse)
                return packet.QueryCreatureResponse.Entry;
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryGameObjectResponse)
                return packet.QueryGameObjectResponse.Entry;
            if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverRequestItems)
                return packet.QuestGiverRequestItems.QuestGiverEntry;
            if (packet.KindCase == PacketHolder.KindOneofCase.QuestAddKillCredit)
                return packet.QuestAddKillCredit.KillCredit;

            var guid = guidExtractor.Process(in packet);
            return guid?.Entry ?? 0;
        }
    }
}