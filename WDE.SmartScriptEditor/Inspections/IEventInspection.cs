using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface IEventInspection
    {
        InspectionResult? Inspect(SmartEvent e);
    }

    public interface IEventInspectionFix
    {
        void Fix(SmartEvent e);
    }
}