using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptDatabaseProvider
    {
        Task<IList<ISmartScriptLine>> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type);
        IEnumerable<IConditionLine> GetConditionsForScript(uint? entry, int entryOrGuid, SmartScriptType type);
        IEnumerable<IConditionLine> GetConditionsForSourceTarget(uint? entry, int entryOrGuid, SmartScriptType type);
        Task<IList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);
    }
}