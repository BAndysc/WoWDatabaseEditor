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

        public IQuestTemplate? GetQuestTemplate(uint entry) => impl.GetQuestTemplate(entry);
        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() => impl.GetAreaTriggerTemplates();
        public IEnumerable<IQuestTemplate> GetQuestTemplates() => impl.GetQuestTemplates();
        public Task<IQuestRequestItem?> GetQuestRequestItem(uint entry) => impl.GetQuestRequestItem(entry);
        public Task<IList<IItem>?> GetItemTemplatesAsync() => impl.GetItemTemplatesAsync();
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions) => impl.FindSmartScriptLinesBy(conditions);

        public Task<IList<ISpawnGroupTemplate>?> GetSpawnGroupTemplatesAsync() => impl.GetSpawnGroupTemplatesAsync();

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
        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IList<ISmartScriptLine> script) =>
            impl.InstallScriptFor(entryOrGuid, type, script);

        public Task InstallConditions(IEnumerable<IConditionLine> conditions,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null) =>
            impl.InstallConditions(conditions, keyMask, manualKey);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) =>
            impl.GetConditionsFor(sourceType, sourceEntry, sourceId);

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey key) =>
            impl.GetConditionsForAsync(keyMask, key);

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => impl.GetSpellScriptNames(spellId);

        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => impl.GetProjectItems();
        
        public IEnumerable<ISmartScriptProject> GetProjects() => impl.GetProjects();

        public Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            impl.GetSmartScriptEntriesByType(scriptType);
        
        public IBroadcastText? GetBroadcastTextByText(string text) => impl.GetBroadcastTextByText(text);
        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => impl.GetBroadcastTextByTextAsync(text);
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => impl.GetBroadcastTextByIdAsync(id);

        public ICreature? GetCreatureByGuid(uint guid) => impl.GetCreatureByGuid(guid);
        public IGameObject? GetGameObjectByGuid(uint guid) => impl.GetGameObjectByGuid(guid);
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => impl.GetCreaturesByEntry(entry);
        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => impl.GetGameObjectsByEntry(entry);
        public IEnumerable<ICreature> GetCreatures() => impl.GetCreatures();
        public Task<IList<ICreature>> GetCreaturesByMapAsync(uint map) => impl.GetCreaturesByMapAsync(map);
        public Task<IList<IGameObject>> GetGameObjectsByMapAsync(uint map) => impl.GetGameObjectsByMapAsync(map);

        public IEnumerable<IGameObject> GetGameObjects() => impl.GetGameObjects();

        public IEnumerable<ICoreCommandHelp> GetCommands() => impl.GetCommands();
        public Task<IList<ITrinityString>> GetStringsAsync() => impl.GetStringsAsync();
        public Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync() => impl.GetSpellDbcAsync();
    }
}