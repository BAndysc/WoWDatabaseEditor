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
        
        public Task<IList<ISmartScriptLine>> GetScriptFor(int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetScriptForAsync(entryOrGuid, type);
        }
        
        public IEnumerable<IConditionLine> GetConditionsForScript(int entryOrGuid, SmartScriptType type)
        {
            return databaseProvider.GetConditionsFor(SmartConstants.ConditionSourceSmartScript, entryOrGuid, (int)type);
        }
        
        public Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions)
        {
            return databaseProvider.FindSmartScriptLinesBy(conditions);
        }

        public IEnumerable<IConditionLine> GetConditionsForSourceTarget(int entryOrGuid, SmartScriptType type) => Enumerable.Empty<IConditionLine>();
    }
}