using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public interface IEventInspection
    {
        InspectionResult? Inspect(EventAiEvent e);
    }

    public interface IEventInspectionFix
    {
        void Fix(EventAiEvent e);
    }
}