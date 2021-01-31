using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services.MessageBox;
using WDE.Module.Attributes;
using WDE.TrinityMySqlDatabase.Database;
using WDE.TrinityMySqlDatabase.Providers;

namespace WDE.TrinityMySqlDatabase
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseProvider : IDatabaseProvider
    {
        private IDatabaseProvider impl;

        public DatabaseProvider(CachedDatabaseProvider cachedDatabase, 
            NullDatabaseProvider nullDatabaseProvider,
            IConnectionSettingsProvider settings,
            IMessageBoxService messageBoxService)
        {
            if (settings.GetSettings().IsEmpty)
                impl = nullDatabaseProvider;
            else
            {
                try
                {
                    cachedDatabase.TryConnect();
                    impl = cachedDatabase;
                }
                catch (Exception e)
                {
                    impl = nullDatabaseProvider;
                    messageBoxService.ShowDialog(new MessageBoxFactory<bool>().SetTitle("Database error")
                        .SetIcon(MessageBoxIcon.Error)
                        .SetMainInstruction("Couldn't connect to the database")
                        .SetContent(e.Message)
                        .WithOkButton(true)
                        .Build());
                }
            }
        }
        
        public ICreatureTemplate GetCreatureTemplate(uint entry) => impl.GetCreatureTemplate(entry);
        public IEnumerable<ICreatureTemplate> GetCreatureTemplates() => impl.GetCreatureTemplates();
        public IGameObjectTemplate GetGameObjectTemplate(uint entry) => impl.GetGameObjectTemplate(entry);
        public IEnumerable<IGameObjectTemplate> GetGameObjectTemplates() => impl.GetGameObjectTemplates();
        public IQuestTemplate GetQuestTemplate(uint entry) => impl.GetQuestTemplate(entry);
        public IEnumerable<IQuestTemplate> GetQuestTemplates() => impl.GetQuestTemplates();
        public IEnumerable<IGameEvent> GetGameEvents() => impl.GetGameEvents();
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
    }
}