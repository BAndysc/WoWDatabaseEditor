using System.Collections.Generic;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptExporter
    {
        IReadOnlyList<ICondition> ToDatabaseCompatibleConditions(SmartScript script, SmartEvent @event);
        IReadOnlyList<IConditionLine> ToDatabaseCompatibleConditions(SmartScript script, SmartEvent @event, int eventId);
        (ISmartScriptLine[], IConditionLine[]?) ToDatabaseCompatibleSmartScript(SmartScript script);
        IQuery GenerateSql(ISmartScriptSolutionItem item, SmartScript script);
        int GetDatabaseScriptTypeId(SmartScriptType type);
        SmartScriptType GetScriptTypeFromId(int id);
    }
}