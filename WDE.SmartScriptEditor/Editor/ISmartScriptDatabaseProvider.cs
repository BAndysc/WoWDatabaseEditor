using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptDatabaseProvider
    {
        Task<IList<ISmartScriptLine>> GetScriptFor(int entryOrGuid, SmartScriptType type);
        IEnumerable<IConditionLine> GetConditionsForScript(int entryOrGuid, SmartScriptType type);
        IEnumerable<IConditionLine> GetConditionsForSourceTarget(int entryOrGuid, SmartScriptType type);
        Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);
    }
}