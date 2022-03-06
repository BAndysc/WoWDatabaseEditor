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
        IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates();

        IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats();

        IQuestTemplate? GetQuestTemplate(uint entry);
        IEnumerable<IQuestTemplate> GetQuestTemplates();
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

        Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IList<ISmartScriptLine> script);
        Task InstallConditions(IEnumerable<IConditionLine> conditions, ConditionKeyMask keyMask, ConditionKey? manualKey = null);

        IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId);

        Task<IList<IConditionLine>> GetConditionsForAsync(ConditionKeyMask keyMask, ConditionKey manualKey);
        
        IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId);

        Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType);

        IEnumerable<ISmartScriptProjectItem> GetProjectItems();
        IEnumerable<ISmartScriptProject> GetProjects();

        IBroadcastText? GetBroadcastTextByText(string text);
        Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text);
        Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id);

        ICreature? GetCreatureByGuid(uint guid);
        IGameObject? GetGameObjectByGuid(uint guid);
        IEnumerable<ICreature> GetCreaturesByEntry(uint entry);
        IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry);
        IEnumerable<ICreature> GetCreatures();
        Task<IList<ICreature>> GetCreaturesByMapAsync(uint map);
        Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map);
        IEnumerable<IGameObject> GetGameObjects();

        IEnumerable<ICoreCommandHelp> GetCommands();
        Task<IList<ITrinityString>> GetStringsAsync();
        Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync();
        Task<IList<ISpawnGroupTemplate>?> GetSpawnGroupTemplatesAsync() => Task.FromResult<IList<ISpawnGroupTemplate>?>(null);

        Task<IList<IItem>?> GetItemTemplatesAsync() => Task.FromResult<IList<IItem>?>(null);
        
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
    
}