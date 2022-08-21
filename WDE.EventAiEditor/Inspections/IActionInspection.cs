using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public interface IActionInspection
    {
        InspectionResult? Inspect(EventAiAction a);
    }

    public interface IActionInspectionFix
    {
        void Fix(EventAiAction action);
    }
}