using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptDatabaseProvider
    {
        Task<IReadOnlyList<ISmartScriptLine>> GetScriptFor(uint entry, int entryOrGuid, SmartScriptType type);
        Task<IReadOnlyList<IConditionLine>> GetConditionsForScript(uint? entry, int entryOrGuid, SmartScriptType type);
        Task<IReadOnlyList<IConditionLine>> GetConditionsForSourceTarget(uint? entry, int entryOrGuid, SmartScriptType type);
        Task<IReadOnlyList<ISmartScriptLine>> FindSmartScriptLinesBy(IEnumerable<(IDatabaseProvider.SmartLinePropertyType what, int whatValue, int parameterIndex, long valueToSearch)> conditions);
    }
}