using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database.World
{
    public class NullWorldDatabaseProvider : IDatabaseProvider
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
        
        public IEnumerable<INpcText> GetNpcTexts() => Enumerable.Empty<INpcText>();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type) => Enumerable.Empty<ISmartScriptLine>();

        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script) => Task.FromResult(false);

        public Task InstallConditions(IEnumerable<IConditionLine> conditions, IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey? manualKey = null) => Task.FromResult(false);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) => Enumerable.Empty<IConditionLine>();

        public Task<IList<IConditionLine>> GetConditionsForAsync(IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey manualKey) => Task.FromResult<IList<IConditionLine>>(System.Array.Empty<IConditionLine>());
        
        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => Enumerable.Empty<ISpellScriptName>();
        
        public IEnumerable<ISmartScriptProjectItem> GetProjectItems() => Enumerable.Empty<ISmartScriptProjectItem>();
        
        public IEnumerable<ISmartScriptProject> GetProjects() => Enumerable.Empty<ISmartScriptProject>();

        public Task<IList<int>> GetSmartScriptEntriesByType(SmartScriptType scriptType) =>
            Task.FromResult<IList<int>>(new List<int>());
        
        public IBroadcastText? GetBroadcastTextByText(string text) => null;
        
        public ICreature? GetCreatureByGuid(uint guid) => null;

        public IGameObject? GetGameObjectByGuid(uint guid) => null;

        public IEnumerable<ICoreCommandHelp> GetCommands() => Enumerable.Empty<ICoreCommandHelp>();
    }
}
