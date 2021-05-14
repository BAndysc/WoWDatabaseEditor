using System;
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
            if (e.GetParameter(pram).Value != 0 || (e.Flags.Value & (int)SmartEventFlag.NotRepeatable) != 0)
                return null;
            return new InspectionResult()
            {
                Line = e.LineId,
                Message = $"`{parameterName}` is 0, the event will be triggered only once. If this is on purpose, add NOT_REPEATABLE flag.",
                Severity = DiagnosticSeverity.Error
            };
        }

        public InspectionResult? Inspect(SmartBaseElement element) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
        public InspectionResult? Inspect(SmartAction a) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
        public InspectionResult? Inspect(SmartSource e) => throw new Exception("Repeat validator is not suported for actions, because it doesn't make any sense");
    }
}