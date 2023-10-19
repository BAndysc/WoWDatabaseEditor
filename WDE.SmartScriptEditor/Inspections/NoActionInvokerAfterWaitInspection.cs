using WDE.Common.Managers;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class NoActionInvokerAfterWaitInspection : IEventInspection
    {
        public InspectionResult? Inspect(SmartEvent e)
        {
            bool wasWait = false;
            foreach (var a in e.Actions)
            {
                if (a.Id == SmartConstants.ActionWait)
                    wasWait = true;

                if (!wasWait)
                    continue;

                if (a.Source.Id != SmartConstants.SourceNone)
                {
                    var sourceData = e.Parent?.TryGetSourceData(a.Source);
                    if (sourceData != null && sourceData.IsInvoker)
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "You may not use action invoker after wait action, core limitation"
                        };
                }
                if (a.Target.Id != SmartConstants.TargetNone)
                {
                    var sourceData = e.Parent?.TryGetSourceData(a.Target);
                    if (sourceData != null && sourceData.IsInvoker)
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Error,
                            Line = a.VirtualLineId,
                            Message = "You may not use action invoker after wait action, core limitation"
                        };
                }
            }

            return null;
        }
    }
}