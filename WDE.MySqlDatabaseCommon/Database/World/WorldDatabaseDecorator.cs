using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.DBC;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class WorldDatabaseDecorator : ICachedDatabaseProvider
    {
        protected ICachedDatabaseProvider impl;

        public WorldDatabaseDecorator(ICachedDatabaseProvider provider)
        {
            impl = provider;
        }

        public bool IsConnected => impl.IsConnected;
        public Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync()
        {
            return impl.GetCreatureTemplatesAsync();
        }

        public Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync()
        {
            return impl.GetConversationTemplatesAsync();
        }

        public Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync()
        {
            return impl.GetGameEventsAsync();
        }

        public Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync()
        {
            return impl.GetGameObjectTemplatesAsync();
        }

        public Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync()
        {
            return impl.GetQuestTemplatesAsync();
        }

        public Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int zoneSortId)
        {
            return impl.GetQuestTemplatesBySortIdAsync(zoneSortId);
        }

        public Task<IReadOnlyList<INpcText>> GetNpcTextsAsync()
        {
            return impl.GetNpcTextsAsync();
        }

        public Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync()
        {
            return impl.GetCreatureClassLevelStatsAsync();
        }

        public Task<ICreatureTemplate?> GetCreatureTemplate(uint entry) => impl.GetCreatureTemplate(entry);

        public ICreatureTemplate? GetCachedCreatureTemplate(uint entry) => impl.GetCachedCreatureTemplate(entry);

        public ICreature? GetCachedCreatureByGuid(uint entry, uint guid)
        {
            return impl.GetCachedCreatureByGuid(entry, guid);
        }

        public IGameObject? GetCachedGameObjectByGuid(uint entry, uint guid)
        {
            return impl.GetCachedGameObjectByGuid(entry, guid);
        }

        public IReadOnlyList<ICreatureTemplate>? GetCachedCreatureTemplates() => impl.GetCachedCreatureTemplates();

        public Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry) => impl.GetCreatureTemplateDifficulties(entry);

        public Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry) => impl.GetGameObjectTemplate(entry);
        public IGameObjectTemplate? GetCachedGameObjectTemplate(uint entry) => impl.GetCachedGameObjectTemplate(entry);
        public IReadOnlyList<IGameObjectTemplate>? GetCachedGameObjectTemplates()
        {
            return impl.GetCachedGameObjectTemplates();
        }

        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => impl.GetAreaTriggerScript(entry);
        public Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => impl.GetAreaTriggerTemplate(entry);

        public Task<IQuestTemplate?> GetQuestTemplate(uint entry) => impl.GetQuestTemplate(entry);
        public IQuestTemplate? GetCachedQuestTemplate(uint entry) => impl.GetCachedQuestTemplate(entry);
        public ISceneTemplate? GetCachedSceneTemplate(uint entry) => impl.GetCachedSceneTemplate(entry);

        public async Task WaitForCache() => await impl.WaitForCache();

        public Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync() => impl.GetAreaTriggerTemplatesAsync();

        public async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId) => await impl.GetQuestObjectives(questId);
        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => await impl.GetQuestObjective(questId, storageIndex);
        public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => await impl.GetQuestObjectiveById(objectiveId);

        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => impl.GetQuestRequestItem(entry);
        public Task<IReadOnlyList<IItem>?> GetItemTemplatesAsync() => impl.GetItemTemplatesAsync();
        public Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => impl.FindSmartScriptLinesBy(conditions);

        public Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => impl.GetSpawnGroupTemplatesAsync();
        public Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => impl.GetSpawnGroupSpawnsAsync();
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => impl.GetSpawnGroupTemplateByIdAsync(id);
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => impl.GetSpawnGroupSpawnByGuidAsync(guid, type);
        public Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => impl.GetSpawnGroupFormation(id);
        public Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => impl.GetSpawnGroupFormations();
        
        public Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) => impl.GetGossipMenuOptionsAsync(menuId);
        public Task<INpcText?> GetNpcText(uint entry) => impl.GetNpcText(entry);

        public Task<IGossipMenu?> GetGossipMenuAsync(uint menuId) => impl.GetGossipMenuAsync(menuId);

        public Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync() => impl.GetPointsOfInterestsAsync();
        public Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync() => impl.GetBroadcastTextsAsync();

        public Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => impl.GetCreatureTextsByEntryAsync(entry);

        public Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => impl.GetLinesCallingSmartTimedActionList(timedActionList);

        public Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync() => impl.GetGossipMenusAsync();

        public async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) => await impl.GetScriptForAsync(entry, entryOrGuid, type);

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId) => await impl.GetConditionsForAsync(sourceType, sourceEntry, sourceId);

        public Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key) =>
            impl.GetConditionsForAsync(keyMask, key);

        public Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys) => 
            impl.GetConditionsForAsync(keyMask, manualKeys);

        public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId) => await impl.GetSpellScriptNames(spellId);

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => await impl.GetPlayerChoiceResponsesAsync(choiceId);

        public Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            impl.GetSmartScriptEntriesByType(scriptType);

        public async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync() => await impl.GetPlayerChoicesAsync();

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => await impl.GetPlayerChoiceResponsesAsync();

        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => impl.GetBroadcastTextByTextAsync(text);
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => impl.GetBroadcastTextByIdAsync(id);
        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => impl.GetBroadcastTextLocaleByTextAsync(text);

        public Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid) => impl.GetCreatureByGuidAsync(entry, guid);
        public Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry) => impl.GetCreaturesByEntryAsync(entry);
        public Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry) => impl.GetGameObjectsByEntryAsync(entry);

        public Task<IReadOnlyList<ICreature>> GetCreaturesAsync() => impl.GetCreaturesAsync();
        public Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync() => impl.GetGameObjectsAsync();
        public Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map) => impl.GetCreaturesByMapAsync(map);
        public Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map) => impl.GetGameObjectsByMapAsync(map);

        public Task<IReadOnlyList<ICoreCommandHelp>> GetCommands() => impl.GetCommands();
        public Task<IReadOnlyList<ITrinityString>> GetStringsAsync() => impl.GetStringsAsync();
        public Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync() => impl.GetSpellDbcAsync();
        public Task<IReadOnlyList<IDatabaseSpellEffectDbc>> GetSpellEffectDbcAsync() => impl.GetSpellEffectDbcAsync();
        public Task<IReadOnlyList<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) => impl.FindEventScriptLinesBy(conditions);
        public Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync() => impl.GetCreatureModelInfoAsync();

        public Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId) => impl.GetCreatureModelInfo(displayId);

        public Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId) => impl.GetQuestStarters(questId);
        public Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId) => impl.GetQuestEnders(questId);

        public Task<IReadOnlyList<IQuestFactionChange>> GetQuestFactionChanges() => impl.GetQuestFactionChanges();
        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => impl.GetSceneTemplateAsync(sceneId);
        public Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync() => impl.GetSceneTemplatesAsync();
        public Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => impl.GetPhaseNameAsync(phaseId);
        public Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync() => impl.GetPhaseNamesAsync();
        public Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId) => impl.GetNpcSpellClickSpells(creatureId);

        public IList<IPhaseName>? GetPhaseNames() => impl.GetPhaseNames();
        public Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons() => impl.GetCreatureAddons();

        public Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => impl.GetCreatureTemplateAddons();

        public Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => impl.GetCreatureEquipmentTemplates();

        public Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => impl.GetMangosCreatureEquipmentTemplates();
        
        public Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync() => impl.GetGameEventCreaturesAsync();

        public Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => impl.GetGameEventGameObjectsAsync();

        public Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid) => impl.GetGameEventCreaturesByGuidAsync(entry, guid);

        public Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid) => impl.GetGameEventGameObjectsByGuidAsync(entry, guid);

        public Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => impl.GetCreatureEquipmentTemplates(entry);

        public Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid) => impl.GetGameObjectByGuidAsync(entry, guid);

        public Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid) => impl.GetCreaturesByGuidAsync(entry, guid);

        public Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid) => impl.GetCreatureAddon(entry, guid);

        public Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => impl.GetCreatureTemplateAddon(entry);

        public Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => impl.GetWaypointData(pathId);

        public Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count) => impl.GetSmartScriptWaypoints(pathId, count);

        public Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => impl.GetScriptWaypoints(pathId);

        public Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => impl.GetMangosWaypoints(pathId);

        public Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => impl.GetMangosCreatureMovement(guid);
        
        public Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => impl.GetMangosCreatureMovementTemplate(entry, pathId);

        public Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => impl.GetMangosPathName(pathId);

        public Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry) => impl.GetLoot(type, entry);
        
        public Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type) => impl.GetLoot(type);

        public Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry) => impl.GetLootTemplateName(type, entry);
        
        public Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type) => impl.GetLootTemplateName(type);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId) => impl.GetCreatureLootCrossReference(lootId);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId) => impl.GetCreatureSkinningLootCrossReference(lootId);

        public Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId) => impl.GetCreaturePickPocketLootCrossReference(lootId);

        public Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId) => impl.GetGameObjectLootCrossReference(lootId);

        public Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId) => impl.GetReferenceLootCrossReference(lootId);

        public Task<IReadOnlyList<IConversationActor>> GetConversationActors() => impl.GetConversationActors();

        public Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates() => impl.GetConversationActorTemplates();

        public Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => impl.GetEventScript(type, id);
        
        public Task<IReadOnlyList<IEventAiLine>> GetEventAi(int id) => impl.GetEventAi(id);

        public Task<IReadOnlyList<IQuestScriptName>> GetQuestScriptNames(uint questId) => impl.GetQuestScriptNames(questId);
        
        public Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids) => impl.GetCreaturesAsync(guids);

        public Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids) => impl.GetGameObjectsAsync(guids);

    }
}
