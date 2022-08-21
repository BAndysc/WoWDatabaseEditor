using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public interface IEventActionInspection : IEventInspection, IActionInspection
    {
        InspectionResult? Inspect(EventAiBaseElement element);
        
        InspectionResult? Inspect(EventAiAction a) => Inspect((EventAiBaseElement) a);
        InspectionResult? Inspect(EventAiEvent a) => Inspect((EventAiBaseElement) a);
    }

    public interface IEventActionInspectionFix : IEventInspectionFix, IActionInspectionFix
    {
        void Fix(EventAiBaseElement element);
        
        void Fix(EventAiAction a) => Fix((EventAiBaseElement) a);
        void Fix(EventAiEvent a) => Fix((EventAiBaseElement) a);
    }
}