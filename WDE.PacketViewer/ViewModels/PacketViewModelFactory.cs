using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.PacketViewer.Processing.Processors;
using WoWPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.ViewModels
{
    public class PacketViewModelFactory : IPacketProcessor<PacketViewModel>
    {
        private readonly IDatabaseProvider databaseProvider;
        private static EntryExtractorProcessor entryExtractorProcessor = new ();
        private static GuidExtractorProcessor guidExtractorProcessor = new ();

        private Dictionary<uint, string> creatureNames = new();
        private Dictionary<uint, string> gameobjectNames = new();
        private Dictionary<UniversalGuid, string> playerNames = new();

        private UniversalGuid? playerGuid;

        public PacketViewModelFactory(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public PacketViewModel? Process(PacketHolder packet)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
                playerGuid = packet.PlayerLogin.PlayerGuid;
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryGameObjectResponse)
                gameobjectNames[packet.QueryGameObjectResponse.Entry] = packet.QueryGameObjectResponse.Name;
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryCreatureResponse)
                creatureNames[packet.QueryCreatureResponse.Entry] = packet.QueryCreatureResponse.Name;

            if (packet.KindCase == PacketHolder.KindOneofCase.QueryPlayerNameResponse)
                playerNames[packet.QueryPlayerNameResponse.PlayerGuid] = packet.QueryPlayerNameResponse.PlayerName;

            var guid = guidExtractorProcessor.Process(packet);
            var entry = entryExtractorProcessor.Process(packet);

            string? name = null;
            if (guid != null)
            {
                if (guid.Type == UniversalHighGuid.Creature ||
                    guid.Type == UniversalHighGuid.Pet ||
                    guid.Type == UniversalHighGuid.Vehicle)
                    creatureNames.TryGetValue(entry, out name);
                else if (guid.Type == UniversalHighGuid.GameObject ||
                         guid.Type == UniversalHighGuid.WorldTransaction ||
                         guid.Type == UniversalHighGuid.Transport)
                    gameobjectNames.TryGetValue(entry, out name);
                else if (guid.Type == UniversalHighGuid.Player)
                {
                    if (playerNames.TryGetValue(guid, out var playerName))
                        name = "Player: " + playerName;
                }
            }

            if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverAcceptQuest)
            {
                entry = packet.QuestGiverAcceptQuest.QuestId;
                name = databaseProvider.GetQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverRequestItems)
            {
                entry = packet.QuestGiverRequestItems.QuestId;
                name = databaseProvider.GetQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverQuestComplete)
            {
                entry = packet.QuestGiverQuestComplete.QuestId;
                name = databaseProvider.GetQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverCompleteQuestRequest)
            {
                entry = packet.QuestGiverCompleteQuestRequest.QuestId;
                name = databaseProvider.GetQuestTemplate(entry)?.Name;
            }

            if (guid != null && playerGuid != null && guid.Equals(playerGuid))
                name = "You";
            
            return new PacketViewModel(packet, entry, name);
        }
    }
}