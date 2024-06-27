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
        private readonly ICachedDatabaseProvider databaseProvider;
        private readonly ISpellStore spellStore;
        private static EntryExtractorProcessor entryExtractorProcessor = new ();
        private static GuidExtractorProcessor guidExtractorProcessor = new ();

        private Dictionary<uint, string> creatureNames = new();
        private Dictionary<uint, string> gameobjectNames = new();
        private Dictionary<UniversalGuid, string> playerNames = new();

        private UniversalGuid? playerGuid;

        public PacketViewModelFactory(ICachedDatabaseProvider databaseProvider, 
            ISpellStore spellStore)
        {
            this.databaseProvider = databaseProvider;
            this.spellStore = spellStore;
        }
        
        public unsafe PacketViewModel? Process(PacketHolder* packetPtr, int originalId)
        {
            ref PacketHolder packet = ref *packetPtr;
            if (packet.KindCase == PacketHolder.KindOneofCase.PlayerLogin)
                playerGuid = packet.PlayerLogin.PlayerGuid;
            else if (playerGuid == null && packet.KindCase == PacketHolder.KindOneofCase.ClientMove)
                playerGuid = packet.ClientMove.Mover;
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryGameObjectResponse)
                gameobjectNames[packet.QueryGameObjectResponse.Entry] = packet.QueryGameObjectResponse.Name.ToString() ?? "";
            
            if (packet.KindCase == PacketHolder.KindOneofCase.QueryCreatureResponse)
                creatureNames[packet.QueryCreatureResponse.Entry] = packet.QueryCreatureResponse.Name.ToString() ?? "";

            if (packet.KindCase == PacketHolder.KindOneofCase.QueryPlayerNameResponse)
            {
                foreach (var pair in packet.QueryPlayerNameResponse.Responses.AsSpan())
                    if (pair.PlayerGuid != default)
                        playerNames[pair.PlayerGuid] = pair.PlayerName.ToString() ?? "";
            }
 
            var guid = guidExtractorProcessor.Process(in packet);
            var entry = entryExtractorProcessor.Process(in packet);

            string? name = null;
            if (guid != null)
            {
                if (guid.Value.Type == UniversalHighGuid.Creature ||
                    guid.Value.Type == UniversalHighGuid.Pet ||
                    guid.Value.Type == UniversalHighGuid.Vehicle)
                {
                    if (!creatureNames.TryGetValue(entry, out name))
                    {
                        var template = databaseProvider.GetCachedCreatureTemplate(entry);
                        if (template != null)
                            name = creatureNames[entry] = template.Name;
                    }
                }
                else if (guid.Value.Type == UniversalHighGuid.GameObject ||
                         guid.Value.Type == UniversalHighGuid.WorldTransaction ||
                         guid.Value.Type == UniversalHighGuid.Transport)
                {
                    if (!gameobjectNames.TryGetValue(entry, out name))
                    {
                        var template = databaseProvider.GetCachedGameObjectTemplate(entry);
                        if (template != null)
                            name = gameobjectNames[entry] = template.Name;
                    }
                }
                else if (guid.Value.Type == UniversalHighGuid.Player)
                {
                    if (playerNames.TryGetValue(guid.Value, out var playerName))
                        name = "Player: " + playerName;
                }
            }

            if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverAcceptQuest)
            {
                entry = packet.QuestGiverAcceptQuest.QuestId;
                name = databaseProvider.GetCachedQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverRequestItems)
            {
                entry = packet.QuestGiverRequestItems.QuestId;
                name = databaseProvider.GetCachedQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverQuestComplete)
            {
                entry = packet.QuestGiverQuestComplete.QuestId;
                name = databaseProvider.GetCachedQuestTemplate(entry)?.Name;
            }
            else if (packet.KindCase == PacketHolder.KindOneofCase.QuestGiverCompleteQuestRequest)
            {
                entry = packet.QuestGiverCompleteQuestRequest.QuestId;
                name = databaseProvider.GetCachedQuestTemplate(entry)?.Name;
            }

            if (guid != null && playerGuid != null && guid.Equals(playerGuid))
                name = "You";

            if (packet.KindCase == PacketHolder.KindOneofCase.SpellGo
                || packet.KindCase == PacketHolder.KindOneofCase.SpellStart)
            {
                if (packet.KindCase == PacketHolder.KindOneofCase.SpellGo && packet.SpellGo.Data == null || packet.KindCase == PacketHolder.KindOneofCase.SpellStart && packet.SpellStart.Data == null)
                    return null;
                var spellId = packet.KindCase == PacketHolder.KindOneofCase.SpellGo ? packet.SpellGo.Data->Spell : packet.SpellStart.Data->Spell;
                var spellName = spellStore.HasSpell(spellId) ? spellStore.GetName(spellId) : null;
                if (spellName != null)
                {
                    if (name != null)
                        name = name + " / " + spellName;
                    else
                        name = spellName;
                }
            }
            
            return new PacketViewModel(packetPtr, originalId, entry, guid, name);
        }
    }
}