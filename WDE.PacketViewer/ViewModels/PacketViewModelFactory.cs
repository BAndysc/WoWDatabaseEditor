using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.PacketViewer.Processing.Processors;
using WowPacketParser.Proto;
using WowPacketParser.Proto.Processing;

namespace WDE.PacketViewer.ViewModels
{
    [AutoRegister]
    public class PacketViewModelFactory
    {
        private readonly IDatabaseProvider databaseProvider;
        private readonly ISpellStore spellStore;
        private static EntryExtractorProcessor entryExtractorProcessor = new ();
        private static GuidExtractorProcessor guidExtractorProcessor = new ();

        private Dictionary<uint, string> creatureNames = new();
        private Dictionary<uint, string> gameobjectNames = new();
        private Dictionary<UniversalGuid, string> playerNames = new();

        private UniversalGuid? playerGuid;

        public PacketViewModelFactory(IDatabaseProvider databaseProvider, ISpellStore spellStore)
        {
            this.databaseProvider = databaseProvider;
            this.spellStore = spellStore;
        }
        
        public PacketViewModel? Process(PacketHolder packet, int originalId)
        {
            if (packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
                playerGuid = packet.PlayerLogin.PlayerGuid;
            else if (playerGuid == null && packet.KindCase == PacketHolder.KindOneofCase.ClientMove)
                playerGuid = packet.ClientMove.Mover;
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryGameObjectResponse)
                gameobjectNames[packet.QueryGameObjectResponse.Entry] = packet.QueryGameObjectResponse.Name;
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryCreatureResponse)
                creatureNames[packet.QueryCreatureResponse.Entry] = packet.QueryCreatureResponse.Name;

            if (packet.KindCase == PacketHolder.KindOneofCase.QueryPlayerNameResponse)
            {
                foreach (var pair in packet.QueryPlayerNameResponse.Responses)
                    playerNames[pair.PlayerGuid] = pair.PlayerName;
            }
 
            var guid = guidExtractorProcessor.Process(packet);
            var entry = entryExtractorProcessor.Process(packet);

            string? name = null;
            if (guid != null)
            {
                if (guid.Type == UniversalHighGuid.Creature ||
                    guid.Type == UniversalHighGuid.Pet ||
                    guid.Type == UniversalHighGuid.Vehicle)
                {
                    if (!creatureNames.TryGetValue(entry, out name))
                    {
                        var template = databaseProvider.GetCreatureTemplate(entry);
                        if (template != null)
                            name = creatureNames[entry] = template.Name;
                    }
                }
                else if (guid.Type == UniversalHighGuid.GameObject ||
                         guid.Type == UniversalHighGuid.WorldTransaction ||
                         guid.Type == UniversalHighGuid.Transport)
                {
                    if (!gameobjectNames.TryGetValue(entry, out name))
                    {
                        var template = databaseProvider.GetGameObjectTemplate(entry);
                        if (template != null)
                            name = gameobjectNames[entry] = template.Name;
                    }
                }
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

            if (packet.KindCase == PacketHolder.KindOneofCase.SpellGo
                || packet.KindCase == PacketHolder.KindOneofCase.SpellStart)
            {
                var spellId = packet.SpellGo?.Data.Spell ?? packet.SpellStart.Data.Spell;
                var spellName = spellStore.HasSpell(spellId) ? spellStore.GetName(spellId) : null;
                if (spellName != null)
                {
                    if (name != null)
                        name = name + " / " + spellName;
                    else
                        name = spellName;
                }
            }
            
            return new PacketViewModel(packet, originalId, entry, guid, name);
        }
    }
}