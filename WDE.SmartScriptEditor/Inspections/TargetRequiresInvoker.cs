using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class TargetRequiresInvoker : ITargetInspection
    {
        public InspectionResult? Inspect(SmartSource a)
        {
            SmartEvent? parent = a.Parent?.Parent;
            SmartScriptBase? script = parent?.Parent;
            if (parent == null || script == null)
                return null;

            if (script.SourceType == SmartScriptType.TimedActionList)
                return null;

            if (parent.Id == SmartConstants.EventLink)
                return null;
            
            var parentEventData = script.GetEventData(parent);

            if (parentEventData.Invoker != null)
                return null;

            var isSource = a.Parent != null && a.Parent.Source == a;
            
            return new InspectionResult()
            {
                Line = a.Parent?.VirtualLineId ?? -1,
                Severity = DiagnosticSeverity.Error,
                Message = "Used " + (isSource ? "source" : "target") + " requires action invoker, but event `" + parentEventData.NameReadable + "` doesn't provide one."
            };
        }
    }
}