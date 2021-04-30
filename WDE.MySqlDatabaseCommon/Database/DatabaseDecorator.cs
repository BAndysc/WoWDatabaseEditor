using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.Database
{
    public class DatabaseDecorator : IDatabaseProvider
    {
        protected IDatabaseProvider impl;

        public DatabaseDecorator(IDatabaseProvider provider)
        {
            impl = provider;
        }

        public bool IsConnected => impl.IsConnected;
        public ICreatureTemplate GetCreatureTemplate(uint entry) => impl.GetCreatureTemplate(entry);
        public IEnumerable<ICreatureTemplate> GetCreatureTemplates() => impl.GetCreatureTemplates();
        public IGameObjectTemplate GetGameObjectTemplate(uint entry) => impl.GetGameObjectTemplate(entry);
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates() => impl.GetGameObjectTemplates();
        public IQuestTemplate GetQuestTemplate(uint entry) => impl.GetQuestTemplate(entry);
        public IEnumerable<IAreaTriggerTemplate> GetAreaTriggerTemplates() => impl.GetAreaTriggerTemplates();
        public IEnumerable<IQuestTemplate> GetQuestTemplates() => impl.GetQuestTemplates();
        public IEnumerable<IGameEvent> GetGameEvents() => impl.GetGameEvents();
        public IEnumerable<IConversationTemplate> GetConversationTemplates() => impl.GetConversationTemplates();
        public IEnumerable<INpcText> GetNpcTexts() => impl.GetNpcTexts();
        public IEnumerable<IGossipMenu> GetGossipMenus() => impl.GetGossipMenus();
        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type) =>
            impl.GetScriptFor(entryOrGuid, type);
        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script) =>
            impl.InstallScriptFor(entryOrGuid, type, script);

        public Task InstallConditions(IEnumerable<IConditionLine> conditions,
            IDatabaseProvider.ConditionKeyMask keyMask,
            IDatabaseProvider.ConditionKey? manualKey = null) =>
            impl.InstallConditions(conditions, keyMask, manualKey);

        public IEnumerable<IConditionLine> GetConditionsFor(int sourceType, int sourceEntry, int sourceId) =>
            impl.GetConditionsFor(sourceType, sourceEntry, sourceId);

        public IEnumerable<ISpellScriptName> GetSpellScriptNames(int spellId) => impl.GetSpellScriptNames(spellId);
    }
}