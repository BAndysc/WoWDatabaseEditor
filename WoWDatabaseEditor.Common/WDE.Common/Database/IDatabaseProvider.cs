using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.DBC;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IDatabaseProvider
    {
        bool IsConnected { get; }
        
        ICreatureTemplate? GetCreatureTemplate(uint entry);
        IEnumerable<ICreatureTemplate> GetCreatureTemplates();

        IGameObjectTemplate? GetGameObjectTemplate(uint entry);
        IEnumerable<IGameObjectTemplate> GetGameObjectTemplates();

        Task<IAreaTriggerScript?> GetAreaTriggerScript(int entry);
        Task<IAreaTriggerTemplate?> GetAreaTriggerTemplate(int entry);
        IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates();
        Task<IList<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync();

        IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats();

        IQuestTemplate? GetQuestTemplate(uint entry);
        IEnumerable<IQuestTemplate> GetQuestTemplates();
        Task<IList<IQuestObjective>> GetQuestObjectives(uint questId);
        Task<IQuestObjective?> GetQuestObjective(uint questId, int storageIndex);
        Task<IQuestObjective?> GetQuestObjectiveById(uint objectiveId);
        Task<IQuestRequestItem?> GetQuestRequestItem(uint entry);

        IEnumerable<IGameEvent> GetGameEvents();
        IEnumerable<IConversationTemplate> GetConversationTemplates();
        IEnumerable<IGossipMenu> GetGossipMenus();
        Task<List<IGossipMenu>> GetGossipMenusAsync();
        List<IGossipMenuOption> GetGossipMenuOptions(uint menuId);
        Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId);
        IEnumerable<INpcText> GetNpcTexts();
        INpcText? GetNpcText(uint entry);
        Task<List<IPointOfInterest>> GetPointsOfInterestsAsync();

        Task<List<ICreatureText>> GetCreatureTextsByEntryAsync(uint entry);
        IReadOnlyList<ICreatureText>? GetCreatureTextsByEntry(uint entry);
        
        Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList);
        IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type);
        Task<IList<ISmartScriptLine>> GetScriptForAsync(int entryOrGuid, SmartScriptType type);

        Task InstallConditions(IEnumerable<IConditionLine> conditions, ConditionKeyMask keyMask, ConditionKey? manualKey = null);

        IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId);

        Task<IList<IConditionLine>> GetConditionsForAsync(ConditionKeyMask keyMask, ConditionKey manualKey);
        
        IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId);

        Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType);

        Task<IList<IPlayerChoice>?> GetPlayerChoicesAsync();
        Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync();
        Task<IList<IPlayerChoiceResponse>?> GetPlayerChoiceResponsesAsync(int choiceId);

        IEnumerable<ISmartScriptProjectItem> GetLegacyProjectItems();
        IEnumerable<ISmartScriptProject> GetLegacyProjects();

        IBroadcastText? GetBroadcastTextByText(string text);
        Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text);
        Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id);

        Task<IBroadcastTextLocale?> GetBroadcastTextLocaleByTextAsync(string text);

        ICreature? GetCreatureByGuid(uint guid);
        IGameObject? GetGameObjectByGuid(uint guid);
        IEnumerable<ICreature> GetCreaturesByEntry(uint entry);
        IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        Task<IList<ICreature>> GetCreaturesByEntryAsync(uint entry);
        Task<IList<IGameObject>> GetGameObjectsByEntryAsync(uint entry);
        IEnumerable<ICreature> GetCreatures();
        Task<IList<ICreature>> GetCreaturesAsync();
        Task<IList<IGameObject>> GetGameObjectsAsync();
        Task<IList<ICreature>> GetCreaturesByMapAsync(uint map);
        Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map);
        IEnumerable<IGameObject> GetGameObjects();

        IEnumerable<ICoreCommandHelp> GetCommands();
        Task<IList<ITrinityString>> GetStringsAsync();
        Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync();
        Task<IList<ISpawnGroupTemplate>> GetSpawnGroupTemplatesAsync();
        Task<IList<ISpawnGroupSpawn>> GetSpawnGroupSpawnsAsync();
        Task<ISpawnGroupTemplate?> GetSpawnGroupTemplateByIdAsync(uint id);
        Task<ISpawnGroupSpawn?> GetSpawnGroupSpawnByGuidAsync(uint guid, SpawnGroupTemplateType type);
        Task<ISpawnGroupFormation?> GetSpawnGroupFormation(uint id);
        Task<IList<ISpawnGroupFormation>?> GetSpawnGroupFormations();

        Task<IList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IList<IItem>?>(null);

        Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);

        Task<List<IEventScriptLine>> GetEventScript(EventScriptType type, uint id) => Task.FromResult(new List<IEventScriptLine>());
        Task<List<IEventScriptLine>> FindEventScriptLinesBy(IReadOnlyList<(uint command, int dataIndex, long valueToSearch)> conditions) => Task.FromResult(new List<IEventScriptLine>());

        Task<List<IEventAiLine>> GetEventAi(int id) => Task.FromResult(new List<IEventAiLine>());

        Task<IList<ICreatureModelInfo>> GetCreatureModelInfoAsync();
        ICreatureModelInfo? GetCreatureModelInfo(uint displayId);

        ISceneTemplate? GetSceneTemplate(uint sceneId);
        Task<ISceneTemplate?> GetSceneTemplateAsync(uint sceneId);
        Task<IList<ISceneTemplate>?> GetSceneTemplatesAsync();
        
        Task<IList<ICreatureAddon>> GetCreatureAddons();
        Task<IList<ICreatureTemplateAddon>> GetCreatureTemplateAddons();
        Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates();
        Task<IList<IMangosCreatureEquipmentTemplate>?> GetMangosCreatureEquipmentTemplates();
        Task<IList<IGameEventCreature>> GetGameEventCreaturesAsync();
        Task<IList<IGameEventGameObject>> GetGameEventGameObjectsAsync();
        Task<IList<IGameEventCreature>?> GetGameEventCreaturesByGuidAsync(uint guid);
        Task<IList<IGameEventGameObject>?> GetGameEventGameObjectsByGuidAsync(uint guid);
        Task<IList<ICreatureEquipmentTemplate>?> GetCreatureEquipmentTemplates(uint entry);
        Task<IGameObject?> GetGameObjectByGuidAsync(uint guid);
        Task<ICreature?> GetCreaturesByGuidAsync(uint guid);
        Task<ICreatureAddon?> GetCreatureAddon(uint guid);
        Task<ICreatureTemplateAddon?> GetCreatureTemplateAddon(uint entry);

        Task<IReadOnlyList<IWaypointData>?> GetWaypointData(uint pathId);
        Task<IReadOnlyList<ISmartScriptWaypoint>?> GetSmartScriptWaypoints(uint pathId);
        Task<IReadOnlyList<IScriptWaypoint>?> GetScriptWaypoints(uint pathId);
        Task<IReadOnlyList<IMangosWaypoint>?> GetMangosWaypoints(uint pathId);
        Task<IReadOnlyList<IMangosCreatureMovement>?> GetMangosCreatureMovement(uint guid);
        Task<IReadOnlyList<IMangosCreatureMovementTemplate>?> GetMangosCreatureMovementTemplate(uint entry, uint? pathId);
        Task<IMangosWaypointsPathName?> GetMangosPathName(uint pathId);
            
        public enum SmartLinePropertyType
        {
            Event,
            Action,
            Target,
            Source
        }
        
        [Flags]
        public enum ConditionKeyMask
        {
            SourceGroup = 1,
            SourceEntry = 2,
            SourceId = 4,
            All = SourceGroup | SourceEntry | SourceId,
            None = 0
        }

        public struct ConditionKey
        {
            public readonly int SourceType;
            public readonly int? SourceGroup;
            public readonly int? SourceEntry;
            public readonly int? SourceId;

            public ConditionKey(int sourceType, int? sourceGroup, int? sourceEntry, int? sourceId)
            {
                SourceType = sourceType;
                SourceGroup = sourceGroup;
                SourceEntry = sourceEntry;
                SourceId = sourceId;
            }
        }
    }

    [UniqueProvider]
    public interface IMangosDatabaseProvider : IDatabaseProvider
    {
        Task<IList<IDbScriptRandomTemplate>?> GetScriptRandomTemplates(uint id, RandomTemplateType type)
        {
            return Task.FromResult<IList<IDbScriptRandomTemplate>?>(null);
        }
        
        Task<ICreatureAiSummon?> GetCreatureAiSummon(uint entry);

        public enum RandomTemplateType
        {
            Text = 0,
            RelayScript = 1
        }
    }
    
}