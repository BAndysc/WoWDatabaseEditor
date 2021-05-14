using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface IActionInspection
    {
        InspectionResult? Inspect(SmartAction a);
    }

    public interface IActionInspectionFix
    {
        void Fix(SmartAction action);
    }
}