using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Data;
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
        
        public Task<IReadOnlyList<ISmartScriptLine>> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetScriptForAsync(entry, entryOrGuid, type);
        }
        
        public Task<IReadOnlyList<IConditionLine>> GetConditionsForScript(uint? entry, int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetConditionsForAsync(SmartConstants.ConditionSourceSmartScript, entryOrGuid, (int)type);
        }
        
        public Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            return databaseProvider.FindSmartScriptLinesBy(conditions);
        }

        public async Task<IReadOnlyList<IConditionLine>> GetConditionsForSourceTarget(uint? entry, int entryOrGuid, SmartScriptType type) => Array.Empty<IConditionLine>();
    }
}