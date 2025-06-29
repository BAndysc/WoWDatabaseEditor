using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class NullWorldDatabaseProvider : IAsyncDatabaseProvider, ICachedDatabaseProvider
    {
        public bool IsConnected => false;
        
        public async Task<ICreatureTemplate?> GetCreatureTemplate(uint entry) => null;

        public ICreatureTemplate? GetCachedCreatureTemplate(uint entry) => null;

        public ICreature? GetCachedCreatureByGuid(uint entry, uint guid) => null;

        public IGameObject? GetCachedGameObjectByGuid(uint entry, uint guid) => null;

        public IReadOnlyList<ICreatureTemplate>? GetCachedCreatureTemplates() => Array.Empty<ICreatureTemplate>();

        public IReadOnlyList<ICreatureTemplate> GetCreatureTemplates() => Array.Empty<ICreatureTemplate>();
        public async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry) => Array.Empty<ICreatureTemplateDifficulty>();
        
        public async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry) => null;
        public IGameObjectTemplate? GetCachedGameObjectTemplate(uint entry) => null;

        public IReadOnlyList<IGameObjectTemplate>? GetCachedGameObjectTemplates() => Array.Empty<IGameObjectTemplate>();

        public IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates() => Array.Empty<IGameObjectTemplate>();
        
        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => Task.FromResult<IAreaTriggerScript?>(null);
        
        public async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => null;

        public async Task<IQuestTemplate?> GetQuestTemplate(uint entry) => null;
        public IQuestTemplate? GetCachedQuestTemplate(uint entry) => null;
        public ISceneTemplate? GetCachedSceneTemplate(uint entry) => null;

        public async Task WaitForCache() { }

        public IReadOnlyList<IQuestTemplate> GetQuestTemplates() => Array.Empty<IQuestTemplate>();
        
        public async Task<IReadOnlyList<IQuestObjective>> GetQuestObjectives(uint questId) => new List<IQuestObjective>();

        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => null;

        public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => null;
        
        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => Task.FromResult<IQuestRequestItem?>(null);

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() => Enumerable.Empty<IAreaTriggerTemplate>();

        public IEnumerable<IGameEvent> GetGameEvents() => Enumerable.Empty<IGameEvent>();
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => Enumerable.Empty<IConversationTemplate>();
        
        public IEnumerable<IGossipMenu> GetGossipMenus() => Enumerable.Empty<IGossipMenu>();
        
        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() => Enumerable.Empty<ICreatureClassLevelStat>();

        public List<IGossipMenuOption> GetGossipMenuOptions(uint menuId) => new();

        public Task<IReadOnlyList<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) =>
            Task.FromResult<IReadOnlyList<IGossipMenuOption>>(new List<IGossipMenuOption>());

        public IEnumerable<INpcText> GetNpcTexts() => Enumerable.Empty<INpcText>();
        public async Task<INpcText?> GetNpcText(uint entry) => null;
        public Task<IReadOnlyList<IPointOfInterest>> GetPointsOfInterestsAsync() => Task.FromResult<IReadOnlyList<IPointOfInterest>>(new List<IPointOfInterest>());
        public Task<IReadOnlyList<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => Task.FromResult<IReadOnlyList<ICreatureText>>(new List<ICreatureText>());
        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry) => null;

        public Task<IReadOnlyList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => Task.FromResult<IReadOnlyList<ISmartScriptLine>>(new List<ISmartScriptLine>());

        public IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type) => Enumerable.Empty<ISmartScriptLine>();

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(int sourceType, int sourceEntry, int sourceId) => Array.Empty<IConditionLine>();

        public Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey manualKey) => Task.FromResult<IReadOnlyList<IConditionLine>>(System.Array.Empty<IConditionLine>());

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys) => new List<IConditionLine>();

        public async Task<IReadOnlyList<ISpellScriptName>> GetSpellScriptNames(int spellId) => Array.Empty<ISpellScriptName>();

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;
        
        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => Enumerable.Empty<ISmartScriptProject>();

        public Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            Task.FromResult<IReadOnlyList<int>>(new List<int>());

        public async Task<IReadOnlyList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public async Task<IReadOnlyList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public IBroadcastText? GetBroadcastTextByText(string text) => null;
        
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => Task.FromResult<IBroadcastText?>(null);
        
        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => Task.FromResult<IBroadcastTextLocale?>(null);

        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => Task.FromResult<IBroadcastText?>(null);

        public async Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid) => null;

        public async Task<ICreature?> GetCreatureByGuidAsync(uint entry, uint guid) => null;

        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => Enumerable.Empty<ICreature>();

        public Task<IReadOnlyList<IGameObject>> GetGameObjectsByEntryAsync(uint entry) => Task.FromResult<IReadOnlyList<IGameObject>>(new List<IGameObject>());

        public IReadOnlyList<ICreature> GetCreatures() => Array.Empty<ICreature>();
        
        public async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids) => Array.Empty<IGameObject>();

        public Task<IReadOnlyList<ICreature>> GetCreaturesByMapAsync(int map) => Task.FromResult<IReadOnlyList<ICreature>>(new List<ICreature>());
        
        public Task<IReadOnlyList<IGameObject>> GetGameObjectsByMapAsync(int map) => Task.FromResult<IReadOnlyList<IGameObject>>(new List<IGameObject>());

        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => Enumerable.Empty<IGameObject>();

        public Task<IReadOnlyList<ICreature>> GetCreaturesByEntryAsync(uint entry) => Task.FromResult<IReadOnlyList<ICreature>>(new List<ICreature>());

        public IEnumerable<IGameObject> GetGameObjects() => Enumerable.Empty<IGameObject>();

        public async Task<IReadOnlyList<ICoreCommandHelp>> GetCommands() => Array.Empty<ICoreCommandHelp>();

        public Task<IReadOnlyList<ITrinityString>> GetStringsAsync() => Task.FromResult<IReadOnlyList<ITrinityString>>(new List<ITrinityString>());
        
        public Task<IReadOnlyList<IDatabaseSpellDbc>> GetSpellDbcAsync() => Task.FromResult<IReadOnlyList<IDatabaseSpellDbc>>(new List<IDatabaseSpellDbc>());
       
        public Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => Task.FromResult<IReadOnlyList<ISmartScriptLine>>(new List<ISmartScriptLine>());

        public void ConnectOrThrow() { }

        public Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync() => Task.FromResult<IReadOnlyList<ICreatureTemplate>>(new List<ICreatureTemplate>());

        public Task<IReadOnlyList<ICreature>> GetCreaturesAsync() => Task.FromResult<IReadOnlyList<ICreature>>(new List<ICreature>());

        public Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync() => Task.FromResult<IReadOnlyList<IGameObject>>(new List<IGameObject>());
        
        public async Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids) => Array.Empty<ICreature>();

        public Task<IReadOnlyList<IConversationTemplate>> GetConversationTemplatesAsync() => Task.FromResult<IReadOnlyList<IConversationTemplate>>(new List<IConversationTemplate>());
        
        public Task<IReadOnlyList<IGameEvent>> GetGameEventsAsync() => Task.FromResult<IReadOnlyList<IGameEvent>>(new List<IGameEvent>());

        public Task<IReadOnlyList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync() => Task.FromResult<IReadOnlyList<IAreaTriggerTemplate>>(new List<IAreaTriggerTemplate>());

        public Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectTemplatesAsync() => Task.FromResult<IReadOnlyList<IGameObjectTemplate>>(new List<IGameObjectTemplate>());

        public Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesAsync() => Task.FromResult<IReadOnlyList<IQuestTemplate>>(new List<IQuestTemplate>());

        public Task<IReadOnlyList<IQuestTemplate>> GetQuestTemplatesBySortIdAsync(int zoneSortId) => Task.FromResult<IReadOnlyList<IQuestTemplate>>(new List<IQuestTemplate>());

        public Task<IReadOnlyList<IGossipMenu>> GetGossipMenusAsync() => Task.FromResult<IReadOnlyList<IGossipMenu>>(new List<IGossipMenu>());

        public Task<IGossipMenu?> GetGossipMenuAsync(uint menuId) => Task.FromResult<IGossipMenu?>(null);
        
        public Task<IReadOnlyList<INpcText>> GetNpcTextsAsync() => Task.FromResult<IReadOnlyList<INpcText>>(new List<INpcText>());

        public Task<IReadOnlyList<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync() => Task.FromResult<IReadOnlyList<ICreatureClassLevelStat>>(new List<ICreatureClassLevelStat>());

        public Task<IReadOnlyList<IBroadcastText>> GetBroadcastTextsAsync() => Task.FromResult<IReadOnlyList<IBroadcastText>>(new List<IBroadcastText>());

        public async Task<IReadOnlyList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) => new List<ISmartScriptLine>();

        public Task<IReadOnlyList<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => Task.FromResult<IReadOnlyList<IEventScriptLine>>(new List<IEventScriptLine>());
        public Task<IReadOnlyList<ICreatureModelInfo>> GetCreatureModelInfoAsync()
        {
            return Task.FromResult<IReadOnlyList<ICreatureModelInfo>>(new List<ICreatureModelInfo>());
        }

        public async Task<ICreatureModelInfo?> GetCreatureModelInfo(uint displayId)
        {
            return null;
        }

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestStarters(uint questId) => Array.Empty<IQuestRelation>();

        public async Task<IReadOnlyList<IQuestRelation>> GetQuestEnders(uint questId) => Array.Empty<IQuestRelation>();

        public async Task<IReadOnlyList<IQuestFactionChange>> GetQuestFactionChanges() => [];
        public ISceneTemplate? GetSceneTemplate(uint sceneId) => null;
        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => Task.FromResult<ISceneTemplate?>(null);
        public Task<IReadOnlyList<ISceneTemplate>?> GetSceneTemplatesAsync() => Task.FromResult<IReadOnlyList<ISceneTemplate>?>(null);
        public async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => null;
        public async Task<IReadOnlyList<IPhaseName>?> GetPhaseNamesAsync() => null;
        public async Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId) => Array.Empty<INpcSpellClickSpell>();

        public IList<IPhaseName>? GetPhaseNames() => null;

        public async Task<IReadOnlyList<ICreatureAddon>> GetCreatureAddons() => new List<ICreatureAddon>();
        public async Task<IReadOnlyList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => new List<ICreatureTemplateAddon>();
        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => new List<ICreatureEquipmentTemplate>();
        public async Task<IReadOnlyList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => null;

        public async Task<IReadOnlyList<IGameEventCreature>> GetGameEventCreaturesAsync() => new List<IGameEventCreature>();
        public async Task<IReadOnlyList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => new List<IGameEventGameObject>();
        public async Task<IReadOnlyList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid) => new List<IGameEventCreature>();
        public async Task<IReadOnlyList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid) => new List<IGameEventGameObject>();
        public async Task<IReadOnlyList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => new List<ICreatureEquipmentTemplate>();
        public async Task<ICreature?> GetCreaturesByGuidAsync(uint entry, uint guid) => null;
        public async Task<ICreatureAddon?> GetCreatureAddon(uint entry, uint guid) => null;
        public async Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => null;
        public async Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => null;
        public async Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId, uint count) => null;
        public async Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => null;
        public async Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => null;
        public async Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => null;
        public async Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => null;
        public async Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => null;
        public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type, uint entry) => Array.Empty<ILootEntry>();
        public async Task<IReadOnlyList<ILootEntry>> GetLoot(LootSourceType type) => Array.Empty<ILootEntry>();
        public async Task<ILootTemplateName?> GetLootTemplateName(LootSourceType type, uint entry) => null;
        public async Task<IReadOnlyList<ILootTemplateName>> GetLootTemplateName(LootSourceType type) => Array.Empty<ILootTemplateName>();

        public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureLootCrossReference(uint lootId) => (Array.Empty<ICreatureTemplate>(), Array.Empty<ICreatureTemplateDifficulty>());
        public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreatureSkinningLootCrossReference(uint lootId) => (Array.Empty<ICreatureTemplate>(), Array.Empty<ICreatureTemplateDifficulty>());
        public async Task<(IReadOnlyList<ICreatureTemplate>, IReadOnlyList<ICreatureTemplateDifficulty>)> GetCreaturePickPocketLootCrossReference(uint lootId) => (Array.Empty<ICreatureTemplate>(), Array.Empty<ICreatureTemplateDifficulty>());
        public async Task<IReadOnlyList<IGameObjectTemplate>> GetGameObjectLootCrossReference(uint lootId) => Array.Empty<IGameObjectTemplate>();
        public async Task<IReadOnlyList<ILootEntry>> GetReferenceLootCrossReference(uint lootId) => Array.Empty<ILootEntry>();
        public async Task<IReadOnlyList<IConversationActor>> GetConversationActors() => Array.Empty<IConversationActor>();
        public async Task<IReadOnlyList<IConversationActorTemplate>> GetConversationActorTemplates() => Array.Empty<IConversationActorTemplate>();

        public Task<IReadOnlyList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => Task.FromResult<IReadOnlyList<ISpawnGroupTemplate>>(new List<ISpawnGroupTemplate>());
        public Task<IReadOnlyList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => Task.FromResult<IReadOnlyList<ISpawnGroupSpawn>>(new List<ISpawnGroupSpawn>());
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => Task.FromResult<ISpawnGroupTemplate?>(null);
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => Task.FromResult<ISpawnGroupSpawn?>(null);
        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => null;
        public async Task<IReadOnlyList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => null;
    }
}
