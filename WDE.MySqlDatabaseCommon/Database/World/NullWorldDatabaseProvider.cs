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
        public IReadOnlyList<ICreatureTemplate> GetCreatureTemplates() => Array.Empty<ICreatureTemplate>();
        public async Task<IReadOnlyList<ICreatureTemplateDifficulty>> GetCreatureTemplateDifficulties(uint entry) => Array.Empty<ICreatureTemplateDifficulty>();
        
        public async Task<IGameObjectTemplate?> GetGameObjectTemplate(uint entry) => null;
        public IGameObjectTemplate? GetCachedGameObjectTemplate(uint entry) => null;
        public IReadOnlyList<IGameObjectTemplate> GetGameObjectTemplates() => Array.Empty<IGameObjectTemplate>();
        
        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => Task.FromResult<IAreaTriggerScript?>(null);
        
        public async Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => null;

        public async Task<IQuestTemplate?> GetQuestTemplate(uint entry) => null;
        public IQuestTemplate? GetCachedQuestTemplate(uint entry) => null;
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

        public Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) =>
            Task.FromResult(new List<IGossipMenuOption>());

        public IEnumerable<INpcText> GetNpcTexts() => Enumerable.Empty<INpcText>();
        public INpcText? GetNpcText(uint entry) => null;
        public Task<List<IPointOfInterest>> GetPointsOfInterestsAsync() => Task.FromResult(new List<IPointOfInterest>());
        public Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => Task.FromResult(new List<ICreatureText>());
        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry) => null;

        public Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => Task.FromResult<IList<ISmartScriptLine>>(new List<ISmartScriptLine>());

        public IEnumerable<ISmartScriptLine> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type) => Enumerable.Empty<ISmartScriptLine>();

        public Task InstallConditions(IEnumerable<IConditionLine> conditions, IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey? manualKey = null) => Task.FromResult(false);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) => Enumerable.Empty<IConditionLine>();

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey manualKey) => Task.FromResult<IList<IConditionLine>>(System.Array.Empty<IConditionLine>());

        public async Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys) => new List<IConditionLine>();

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => Enumerable.Empty<ISpellScriptName>();

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => null;
        
        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => Enumerable.Empty<ISmartScriptProject>();

        public Task<IReadOnlyList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            Task.FromResult<IReadOnlyList<int>>(new List<int>());

        public async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => null;

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => null;

        public IBroadcastText? GetBroadcastTextByText(string text) => null;
        
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => Task.FromResult<IBroadcastText?>(null);
        
        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => Task.FromResult<IBroadcastTextLocale?>(null);

        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => Task.FromResult<IBroadcastText?>(null);

        public ICreature? GetCreatureByGuid(uint entry, uint guid) => null;

        public IGameObject? GetGameObjectByGuid(uint entry, uint guid) => null;
        
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => Enumerable.Empty<ICreature>();

        public Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry) => Task.FromResult<IList<IGameObject>>(new List<IGameObject>());

        public IReadOnlyList<ICreature> GetCreatures() => Array.Empty<ICreature>();
        
        public async Task<IReadOnlyList<IGameObject>> GetGameObjectsAsync(IEnumerable<SpawnKey> guids) => Array.Empty<IGameObject>();

        public Task<IList<ICreature>> GetCreaturesByMapAsync(uint map) => Task.FromResult<IList<ICreature>>(new List<ICreature>());
        
        public Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map) => Task.FromResult<IList<IGameObject>>(new List<IGameObject>());

        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => Enumerable.Empty<IGameObject>();

        public Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry) => Task.FromResult<IList<ICreature>>(new List<ICreature>());

        public IEnumerable<IGameObject> GetGameObjects() => Enumerable.Empty<IGameObject>();

        public IEnumerable<ICoreCommandHelp> GetCommands() => Enumerable.Empty<ICoreCommandHelp>();

        public Task<IList<ITrinityString>> GetStringsAsync() => Task.FromResult<IList<ITrinityString>>(new List<ITrinityString>());
        
        public Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync() => Task.FromResult<IList<IDatabaseSpellDbc>>(new List<IDatabaseSpellDbc>());
       
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => Task.FromResult<IList<ISmartScriptLine>>(new List<ISmartScriptLine>());

        public void ConnectOrThrow() { }

        public Task<IReadOnlyList<ICreatureTemplate>> GetCreatureTemplatesAsync() => Task.FromResult<IReadOnlyList<ICreatureTemplate>>(new List<ICreatureTemplate>());

        public Task<IList<ICreature>> GetCreaturesAsync() => Task.FromResult<IList<ICreature>>(new List<ICreature>());

        public Task<IList<IGameObject>> GetGameObjectsAsync() => Task.FromResult<IList<IGameObject>>(new List<IGameObject>());
        
        public async Task<IReadOnlyList<ICreature>> GetCreaturesAsync(IEnumerable<SpawnKey> guids) => Array.Empty<ICreature>();

        public Task<List<IConversationTemplate>> GetConversationTemplatesAsync() => Task.FromResult(new List<IConversationTemplate>());
        
        public Task<List<IGameEvent>> GetGameEventsAsync() => Task.FromResult(new List<IGameEvent>());

        public Task<IList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync() => Task.FromResult<IList<IAreaTriggerTemplate>>(new List<IAreaTriggerTemplate>());

        public Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync() => Task.FromResult(new List<IGameObjectTemplate>());

        public Task<List<IQuestTemplate>> GetQuestTemplatesAsync() => Task.FromResult(new List<IQuestTemplate>());

        public Task<List<IGossipMenu>> GetGossipMenusAsync() => Task.FromResult(new List<IGossipMenu>());

        public Task<IGossipMenu?> GetGossipMenuAsync(uint menuId) => Task.FromResult<IGossipMenu?>(null);
        
        public Task<List<INpcText>> GetNpcTextsAsync() => Task.FromResult(new List<INpcText>());

        public Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync() => Task.FromResult(new List<ICreatureClassLevelStat>());

        public Task<List<IBroadcastText>> GetBroadcastTextsAsync() => Task.FromResult(new List<IBroadcastText>());

        public async Task<IList<ISmartScriptLine>> GetScriptForAsync(uint entry, int entryOrGuid, SmartScriptType type) => new List<ISmartScriptLine>();

        public Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => Task.FromResult(new List<IEventScriptLine>());
        public Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync()
        {
            return Task.FromResult<IList<ICreatureModelInfo>>(new List<ICreatureModelInfo>());
        }

        public ICreatureModelInfo? GetCreatureModelInfo(uint displayId)
        {
            return null;
        }
        
        public ISceneTemplate? GetSceneTemplate(uint sceneId) => null;
        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => Task.FromResult<ISceneTemplate?>(null);
        public Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync() => Task.FromResult<IList<ISceneTemplate>?>(null);
        public async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => null;
        public async Task<IList<IPhaseName>?> GetPhaseNamesAsync() => null;
        public async Task<IReadOnlyList<INpcSpellClickSpell>> GetNpcSpellClickSpells(uint creatureId) => Array.Empty<INpcSpellClickSpell>();

        public IList<IPhaseName>? GetPhaseNames() => null;

        public async Task<IList<ICreatureAddon>> GetCreatureAddons() => new List<ICreatureAddon>();
        public async Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => new List<ICreatureTemplateAddon>();
        public async Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => new List<ICreatureEquipmentTemplate>();
        public async Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => null;

        public async Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync() => new List<IGameEventCreature>();
        public async Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => new List<IGameEventGameObject>();
        public async Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint entry, uint guid) => new List<IGameEventCreature>();
        public async Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint entry, uint guid) => new List<IGameEventGameObject>();
        public async Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => new List<ICreatureEquipmentTemplate>();
        public async Task<IGameObject?> GetGameObjectByGuidAsync(uint entry, uint guid) => null;
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

        public Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => Task.FromResult<IList<ISpawnGroupTemplate>>(new List<ISpawnGroupTemplate>());
        public Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => Task.FromResult<IList<ISpawnGroupSpawn>>(new List<ISpawnGroupSpawn>());
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => Task.FromResult<ISpawnGroupTemplate?>(null);
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => Task.FromResult<ISpawnGroupSpawn?>(null);
        public async Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => null;
        public async Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => null;
    }
}
