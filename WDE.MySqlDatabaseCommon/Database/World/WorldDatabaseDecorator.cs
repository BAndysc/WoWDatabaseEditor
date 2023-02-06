using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.DBC;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class WorldDatabaseDecorator : IDatabaseProvider
    {
        protected IDatabaseProvider impl;

        public WorldDatabaseDecorator(IDatabaseProvider provider)
        {
            impl = provider;
        }

        public bool IsConnected => impl.IsConnected;
        public ICreatureTemplate? GetCreatureTemplate(uint entry) => impl.GetCreatureTemplate(entry);
        public IEnumerable<ICreatureTemplate> GetCreatureTemplates() => impl.GetCreatureTemplates();
        public IGameObjectTemplate? GetGameObjectTemplate(uint entry) => impl.GetGameObjectTemplate(entry);
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates() => impl.GetGameObjectTemplates();
        public Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry) => impl.GetAreaTriggerScript(entry);
        public Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry) => impl.GetAreaTriggerTemplate(entry);

        public IQuestTemplate? GetQuestTemplate(uint entry) => impl.GetQuestTemplate(entry);
        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() => impl.GetAreaTriggerTemplates();
        public Task<IList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync() => impl.GetAreaTriggerTemplatesAsync();

        public IEnumerable<IQuestTemplate> GetQuestTemplates() => impl.GetQuestTemplates();
        public async Task<IList<IQuestObjective>> GetQuestObjectives(uint questId) => await impl.GetQuestObjectives(questId);
        public async Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex) => await impl.GetQuestObjective(questId, storageIndex);
        public async Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId) => await impl.GetQuestObjectiveById(objectiveId);

        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => impl.GetQuestRequestItem(entry);
        public Task<IList<IItem>?> GetItemTemplatesAsync() => impl.GetItemTemplatesAsync();
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => impl.FindSmartScriptLinesBy(conditions);

        public Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync() => impl.GetSpawnGroupTemplatesAsync();
        public Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync() => impl.GetSpawnGroupSpawnsAsync();
        public Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id) => impl.GetSpawnGroupTemplateByIdAsync(id);
        public Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type) => impl.GetSpawnGroupSpawnByGuidAsync(guid, type);
        public Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id) => impl.GetSpawnGroupFormation(id);
        public Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations() => impl.GetSpawnGroupFormations();
        
        public IEnumerable<IGameEvent> GetGameEvents() => impl.GetGameEvents();
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => impl.GetConversationTemplates();
        public Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) => impl.GetGossipMenuOptionsAsync(menuId);
        public List<IGossipMenuOption> GetGossipMenuOptions(uint menuId) => impl.GetGossipMenuOptions(menuId);
        public IEnumerable<INpcText> GetNpcTexts() => impl.GetNpcTexts();
        public INpcText? GetNpcText(uint entry) => impl.GetNpcText(entry);
        public Task<List<IPointOfInterest>> GetPointsOfInterestsAsync() => impl.GetPointsOfInterestsAsync();
        public Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry) => impl.GetCreatureTextsByEntryAsync(entry);
        public IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry) => impl.GetCreatureTextsByEntry(entry);

        public Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => impl.GetLinesCallingSmartTimedActionList(timedActionList);

        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() => impl.GetCreatureClassLevelStats();
        public IEnumerable<IGossipMenu> GetGossipMenus() => impl.GetGossipMenus();
        public Task<List<IGossipMenu>> GetGossipMenusAsync() => impl.GetGossipMenusAsync();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type) =>
            impl.GetScriptFor(entryOrGuid, type);

        public async Task<IList<ISmartScriptLine>> GetScriptForAsync(int entryOrGuid, SmartScriptType type) => await impl.GetScriptForAsync(entryOrGuid, type);

        public Task InstallConditions(IEnumerable<IConditionLine> conditions,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null) =>
            impl.InstallConditions(conditions, keyMask, manualKey);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) =>
            impl.GetConditionsFor(sourceType, sourceEntry, sourceId);

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key) =>
            impl.GetConditionsForAsync(keyMask, key);

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, ICollection<IDatabaseProvider.ConditionKey> manualKeys) => 
            impl.GetConditionsForAsync(keyMask, manualKeys);

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => impl.GetSpellScriptNames(spellId);

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId) => await impl.GetPlayerChoiceResponsesAsync(choiceId);

        public IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems() => impl.GetLegacyProjectItems();
        
        public IEnumerable<ISmartScriptProject> GetLegacyProjects() => impl.GetLegacyProjects();

        public Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            impl.GetSmartScriptEntriesByType(scriptType);

        public async Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync() => await impl.GetPlayerChoicesAsync();

        public async Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync() => await impl.GetPlayerChoiceResponsesAsync();

        public IBroadcastText? GetBroadcastTextByText(string text) => impl.GetBroadcastTextByText(text);
        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => impl.GetBroadcastTextByTextAsync(text);
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => impl.GetBroadcastTextByIdAsync(id);
        public Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text) => impl.GetBroadcastTextLocaleByTextAsync(text);

        public ICreature? GetCreatureByGuid(uint guid) => impl.GetCreatureByGuid(guid);
        public IGameObject? GetGameObjectByGuid(uint guid) => impl.GetGameObjectByGuid(guid);
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => impl.GetCreaturesByEntry(entry);
        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => impl.GetGameObjectsByEntry(entry);
        public Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry) => impl.GetCreaturesByEntryAsync(entry);
        public Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry) => impl.GetGameObjectsByEntryAsync(entry);

        public IEnumerable<ICreature> GetCreatures() => impl.GetCreatures();
        public Task<IList<ICreature>> GetCreaturesAsync() => impl.GetCreaturesAsync();
        public Task<IList<IGameObject>> GetGameObjectsAsync() => impl.GetGameObjectsAsync();
        public Task<IList<ICreature>> GetCreaturesByMapAsync(uint map) => impl.GetCreaturesByMapAsync(map);
        public Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map) => impl.GetGameObjectsByMapAsync(map);

        public IEnumerable<IGameObject> GetGameObjects() => impl.GetGameObjects();

        public IEnumerable<ICoreCommandHelp> GetCommands() => impl.GetCommands();
        public Task<IList<ITrinityString>> GetStringsAsync() => impl.GetStringsAsync();
        public Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync() => impl.GetSpellDbcAsync();
        public Task<List<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) => impl.FindEventScriptLinesBy(conditions);
        public Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync() => impl.GetCreatureModelInfoAsync();

        public ICreatureModelInfo? GetCreatureModelInfo(uint displayId) => impl.GetCreatureModelInfo(displayId);
        public ISceneTemplate? GetSceneTemplate(uint sceneId) => impl.GetSceneTemplate(sceneId);
        public Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId) => impl.GetSceneTemplateAsync(sceneId);
        public Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync() => impl.GetSceneTemplatesAsync();
        public async Task<IPhaseName?> GetPhaseNameAsync(uint phaseId) => await impl.GetPhaseNameAsync(phaseId);
        public async Task<IList<IPhaseName>?> GetPhaseNamesAsync() => await impl.GetPhaseNamesAsync();
        public IList<IPhaseName>? GetPhaseNames() => impl.GetPhaseNames();
        public Task<IList<ICreatureAddon>> GetCreatureAddons() => impl.GetCreatureAddons();

        public Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons() => impl.GetCreatureTemplateAddons();

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates() => impl.GetCreatureEquipmentTemplates();

        public Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates() => impl.GetMangosCreatureEquipmentTemplates();
        
        public Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync() => impl.GetGameEventCreaturesAsync();

        public Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync() => impl.GetGameEventGameObjectsAsync();

        public Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint guid) => impl.GetGameEventCreaturesByGuidAsync(guid);

        public Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint guid) => impl.GetGameEventGameObjectsByGuidAsync(guid);

        public Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry) => impl.GetCreatureEquipmentTemplates(entry);

        public Task<IGameObject?> GetGameObjectByGuidAsync(uint guid) => impl.GetGameObjectByGuidAsync(guid);

        public Task<ICreature?> GetCreaturesByGuidAsync(uint guid) => impl.GetCreaturesByGuidAsync(guid);

        public Task<ICreatureAddon?> GetCreatureAddon(uint guid) => impl.GetCreatureAddon(guid);

        public Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry) => impl.GetCreatureTemplateAddon(entry);

        public Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId) => impl.GetWaypointData(pathId);

        public Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId) => impl.GetSmartScriptWaypoints(pathId);

        public Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId) => impl.GetScriptWaypoints(pathId);

        public Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId) => impl.GetMangosWaypoints(pathId);

        public Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid) => impl.GetMangosCreatureMovement(guid);
        
        public Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId) => impl.GetMangosCreatureMovementTemplate(entry, pathId);

        public Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId) => impl.GetMangosPathName(pathId);

        public Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => impl.GetEventScript(type, id);
        
        public Task<List<IEventAiLine>> GetEventAi(int id) => impl.GetEventAi(id);
    }
}