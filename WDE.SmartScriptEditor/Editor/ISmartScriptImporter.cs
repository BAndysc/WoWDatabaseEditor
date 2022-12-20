using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptImporter
    {
        Task Import(SmartScript script, bool doNotTouchIfPossible, IReadOnlyList<ISmartScriptLine> lines, IReadOnlyList<IConditionLine> conditions, IReadOnlyList<IConditionLine>? targetConditions);
        Dictionary<int, List<SmartCondition>> ImportConditions(SmartScriptBase script, IReadOnlyList<IConditionLine> lines);
        List<SmartCondition> ImportConditions(SmartScriptBase script, IReadOnlyList<ICondition> lines);
    }
}