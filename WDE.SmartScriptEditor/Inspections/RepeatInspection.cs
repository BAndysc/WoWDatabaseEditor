using System;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class RepeatInspection : IEventActionInspection
    {
        private readonly int pram;
        private string parameterName;

        public RepeatInspection(SmartGenericJsonData ev, int pram)
        {
            this.pram = pram;
            parameterName = ev.Parameters != null ? ev.Parameters[pram].Name : throw new Exception("non exiting parameter");
        }

        public InspectionResult? Inspect(SmartEvent e)
        {
            if (e.Parent is { SourceType: SmartScriptType.TimedActionList })
                return null;

            if (e.GetParameter(pram).Value == 0 && (e.Flags.Value & SmartConstants.EventFlagNotRepeatable) == 0)
            {
                return new InspectionResult()
                {
                    Line = e.VirtualLineId,
                    Message = $"`{parameterName}` is 0, the event will be triggered only once. If this is on purpose, add NOT_REPEATABLE flag.",
                    Severity = DiagnosticSeverity.Error
                };
            }
            
            if (e.GetParameter(pram).Value != 0 && (e.Flags.Value & SmartConstants.EventFlagNotRepeatable) != 0)
            {
                return new InspectionResult()
                {
                    Line = e.VirtualLineId,
                    Message = $"`{parameterName}` is non zero, but NOT_REPEATABLE is applied, so it will be invoked only once. Remove the flag or fix the timers.",
                    Severity = DiagnosticSeverity.Warning
                };
            }
            return null;
        }

        public InspectionResult? Inspect(SmartBaseElement element) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
        public InspectionResult? Inspect(SmartAction a) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
        public InspectionResult? Inspect(SmartSource e) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
    }
}