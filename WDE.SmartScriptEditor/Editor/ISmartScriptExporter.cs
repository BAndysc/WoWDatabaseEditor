using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;
using WDE.SqlQueryGenerator;

namespace WDE.SmartScriptEditor.Editor.UserControls
{
    public interface ISmartScriptExporter
    {
        (ISmartScriptLine[], IConditionLine[]) ToDatabaseCompatibleSmartScript(SmartScript script);
        IQuery GenerateSql(ISmartScriptSolutionItem item, SmartScript script);
    }
}