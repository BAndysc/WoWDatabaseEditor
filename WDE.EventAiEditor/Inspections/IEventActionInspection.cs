using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public interface IEventActionInspection : IEventInspection, IActionInspection
    {
        InspectionResult? Inspect(EventAiBaseElement element);
    }
}