using System.Collections.Generic;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor;

public interface ISimpleConditionsImporter
{
    Dictionary<int, List<SmartCondition>> ImportConditions(SmartScriptBase script, IReadOnlyList<IConditionLine>? conditions);
    List<SmartCondition> ImportConditions(SmartScriptBase script, IReadOnlyList<ICondition> conditions);
}