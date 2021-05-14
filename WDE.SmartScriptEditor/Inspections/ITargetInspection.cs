using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface ITargetInspection
    {
        InspectionResult? Inspect(SmartSource a);
    }

    public interface ITargetInspectionFix
    {
        void Fix(SmartSource script);
    }
}