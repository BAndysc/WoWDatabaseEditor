using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface IEventActionInspection : IEventInspection, IActionInspection, ITargetInspection
    {
        InspectionResult? Inspect(SmartBaseElement element);
    }
}