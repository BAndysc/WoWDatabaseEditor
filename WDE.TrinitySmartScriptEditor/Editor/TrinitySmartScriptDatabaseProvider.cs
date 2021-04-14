using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Editor
{
    [AutoRegister]
    public class TrinitySmartScriptDatabaseProvider : ISmartScriptDatabaseProvider
    {
        private readonly IDatabaseProvider databaseProvider;

        public TrinitySmartScriptDatabaseProvider(IDatabaseProvider databaseProvider)
        {
            this.databaseProvider = databaseProvider;
        }
        
        public IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetScriptFor(entryOrGuid, type);
        }

        public Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script)
        {
            return databaseProvider.InstallScriptFor(entryOrGuid, type, script);
        }

        public IEnumerable<IConditionLine> GetConditionsForScript(int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetConditionsFor(SmartConstants.ConditionSourceSmartScript, entryOrGuid, (int)type);
        }
        
        public async Task InstallConditionsForScript(IEnumerable<IConditionLine> conditions, int entryOrGuid, SmartScriptType type)
        {
            await databaseProvider.InstallConditions(conditions, 
                IDatabaseProvider.ConditionKeyMask.SourceEntry | IDatabaseProvider.ConditionKeyMask.SourceId,
                new IDatabaseProvider.ConditionKey(SmartConstants.ConditionSourceSmartScript, null, entryOrGuid, (int)type));
        }
    }
}