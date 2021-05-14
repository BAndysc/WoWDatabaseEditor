using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface IEventActionInspection : IEventInspection, IActionInspection, ITargetInspection
    {
        InspectionResult? Inspect(SmartBaseElement element);
        
        InspectionResult? Inspect(SmartAction a) => Inspect((SmartBaseElement) a);
        InspectionResult? Inspect(SmartEvent a) => Inspect((SmartBaseElement) a);
        InspectionResult? Inspect(SmartSource a) => Inspect((SmartBaseElement) a);
    }

    public interface IEventActionInspectionFix : IEventInspectionFix, IActionInspectionFix, ITargetInspectionFix
    {
        void Fix(SmartBaseElement element);
        
        void Fix(SmartAction a) => Fix((SmartBaseElement) a);
        void Fix(SmartEvent a) => Fix((SmartBaseElement) a);
        void Fix(SmartSource a) => Fix((SmartBaseElement) a);
    }
}