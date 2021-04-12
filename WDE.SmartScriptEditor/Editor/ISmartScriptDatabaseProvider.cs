using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptDatabaseProvider
    {
        IEnumerable<ISmartScriptLine> GetScriptFor(int entryOrGuid, SmartScriptType type);
        Task InstallScriptFor(int entryOrGuid, SmartScriptType type, IEnumerable<ISmartScriptLine> script);
        IEnumerable<IConditionLine> GetConditionsForScript(int entryOrGuid, SmartScriptType type);
        Task InstallConditionsForScript(IEnumerable<IConditionLine> conditions, int entryOrGuid, SmartScriptType type);
    }
}