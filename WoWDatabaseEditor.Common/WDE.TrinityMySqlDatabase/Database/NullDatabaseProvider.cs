using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Database
{
    public class NullDatabaseProvider : IDatabaseProvider
    {
        public ICreatureTemplate? GetCreatureTemplate(uint entry) => null;

        public IEnumerable<ICreatureTemplate> GetCreatureTemplates() => Enumerable.Empty<ICreatureTemplate>();

        public IGameObjectTemplate? GetGameObjectTemplate(uint entry) => null;

        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates() => Enumerable.Empty<IGameObjectTemplate>();

        public IQuestTemplate? GetQuestTemplate(uint entry) => null;

        public IEnumerable<IQuestTemplate> GetQuestTemplates() => Enumerable.Empty<IQuestTemplate>();
        
        public IEnumerable<IGameEvent> GetGameEvents() => Enumerable.Empty<IGameEvent>();

        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type) => Enumerable.Empty<ISmartScriptLine>();

        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            return Task.FromResult(false);
        }

        public Task InstallConditions(IEnumerable<IConditionLine> conditions, IDatabaseProvider.ConditionKeyMask keyMask, IDatabaseProvider.ConditionKey? manualKey = null)
        {
            return Task.FromResult(false);
        }

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) => Enumerable.Empty<IConditionLine>();
    }
}
