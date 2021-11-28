﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class NullWorldDatabaseProvider : IAsyncDatabaseProvider
    {
        public bool IsConnected => false;
        
        public ICreatureTemplate? GetCreatureTemplate(uint entry) => null;

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates() => Enumerable.Empty<ICreatureTemplate>();

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry) => null;

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates() => Enumerable.Empty<IGameObjectTemplate>();

        public IQuestTemplate? GetQuestTemplate(uint entry) => null;

        public IEnumerable<IQuestTemplate> GetQuestTemplates() => Enumerable.Empty<IQuestTemplate>();

        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() => Enumerable.Empty<IAreaTriggerTemplate>();

        public IEnumerable<IGameEvent> GetGameEvents() => Enumerable.Empty<IGameEvent>();
        
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => Enumerable.Empty<IConversationTemplate>();
        
        public IEnumerable<IGossipMenu> GetGossipMenus() => Enumerable.Empty<IGossipMenu>();
        
        public IEnumerable<ICreatureClassLevelStat> GetCreatureClassLevelStats() => Enumerable.Empty<ICreatureClassLevelStat>();

        public Task<List<IGossipMenuOption>> GetGossipMenuOptionsAsync(uint menuId) =>
            Task.FromResult(new List<IGossipMenuOption>());

        public IEnumerable<INpcText> GetNpcTexts() => Enumerable.Empty<INpcText>();
        public INpcText? GetNpcText(uint entry) => null;
        public Task<List<IPointOfInterest>> GetPointsOfInterestsAsync() => Task.FromResult(new List<IPointOfInterest>());
        public Task<List<ICreatureText>> GetCreatureTextsByEntry(uint entry) => Task.FromResult(new List<ICreatureText>());
        public Task<IList<ISmartScriptLine>> GetLinesCallingSmartTimedActionList(int timedActionList) => Task.FromResult<IList<ISmartScriptLine>>(new List<ISmartScriptLine>());

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type) => Enumerable.Empty<ISmartScriptLine>();

        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IList<ISmartScriptLine> script) => Task.FromResult(false);

        public Task InstallConditions(IEnumerable<IConditionLine> conditions, IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey? manualKey = null) => Task.FromResult(false);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) => Enumerable.Empty<IConditionLine>();

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey manualKey) => Task.FromResult<IList<IConditionLine>>(System.Array.Empty<IConditionLine>());
        
        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => Enumerable.Empty<ISpellScriptName>();
        
        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        
        public IEnumerable<ISmartScriptProject> GetProjects() => Enumerable.Empty<ISmartScriptProject>();

        public Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            Task.FromResult<IList<int>>(new List<int>());
        
        public IBroadcastText? GetBroadcastTextByText(string text) => null;
        
        public Task<IBroadcastText?> GetBroadcastTextByIdAsync(uint id) => Task.FromResult<IBroadcastText?>(null);
        
        public Task<IBroadcastText?> GetBroadcastTextByTextAsync(string text) => Task.FromResult<IBroadcastText?>(null);

        public ICreature? GetCreatureByGuid(uint guid) => null;

        public IGameObject? GetGameObjectByGuid(uint guid) => null;
        
        public IEnumerable<ICreature> GetCreaturesByEntry(uint entry) => Enumerable.Empty<ICreature>();

        public IEnumerable<ICreature> GetCreatures() => Enumerable.Empty<ICreature>();

        public IEnumerable<IGameObject> GetGameObjectsByEntry(uint entry) => Enumerable.Empty<IGameObject>();

        public IEnumerable<ICoreCommandHelp> GetCommands() => Enumerable.Empty<ICoreCommandHelp>();

        public Task<IList<ITrinityString>> GetStringsAsync() => Task.FromResult<IList<ITrinityString>>(new List<ITrinityString>());
        
        public Task<IList<IDatabaseSpellDbc>> GetSpellDbcAsync() => Task.FromResult<IList<IDatabaseSpellDbc>>(new List<IDatabaseSpellDbc>());
        
        public Task<List<ICreatureTemplate>> GetCreatureTemplatesAsync() => Task.FromResult(new List<ICreatureTemplate>());

        public Task<List<ICreature>> GetCreaturesAsync() => Task.FromResult(new List<ICreature>());

        public Task<List<IConversationTemplate>> GetConversationTemplatesAsync() => Task.FromResult(new List<IConversationTemplate>());
        
        public Task<List<IGameEvent>> GetGameEventsAsync() => Task.FromResult(new List<IGameEvent>());

        public Task<List<IAreaTriggerTemplate>> GetAreaTriggerTemplatesAsync() => Task.FromResult(new List<IAreaTriggerTemplate>());

        public Task<List<IGameObjectTemplate>> GetGameObjectTemplatesAsync() => Task.FromResult(new List<IGameObjectTemplate>());

        public Task<List<IQuestTemplate>> GetQuestTemplatesAsync() => Task.FromResult(new List<IQuestTemplate>());

        public Task<List<IGossipMenu>> GetGossipMenusAsync() => Task.FromResult(new List<IGossipMenu>());

        public Task<List<INpcText>> GetNpcTextsAsync() => Task.FromResult(new List<INpcText>());

        public Task<List<ICreatureClassLevelStat>> GetCreatureClassLevelStatsAsync() => Task.FromResult(new List<ICreatureClassLevelStat>());

        public Task<List<IBroadcastText>> GetBroadcastTextsAsync() => Task.FromResult(new List<IBroadcastText>());

    }
}
