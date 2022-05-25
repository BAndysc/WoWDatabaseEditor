using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class AwaitAfterAsyncInspection : IEventInspection
    {
        private HashSet<int> asyncActions = new();
        private int beginAwait = -1;
        private int finalizeAwait = -1;
            
        public AwaitAfterAsyncInspection(ISmartDataManager smartDataManager)
        {
            foreach (var a in smartDataManager.GetAllData(SmartType.SmartAction))
            {
                if (a.Flags.HasFlagFast(ActionFlags.Async))
                    asyncActions.Add(a.Id);
                if (a.Flags.HasFlagFast(ActionFlags.BeginAwait))
                    beginAwait = a.Id;
                if (a.Flags.HasFlagFast(ActionFlags.Await))
                    finalizeAwait = a.Id;
            }
        }
        
        public InspectionResult? Inspect(SmartEvent e)
        {
            bool inAwaitBlock = false;
            for (int i = 0; i < e.Actions.Count; ++i)
            {
                bool nextIsAwait = i < e.Actions.Count - 1 && e.Actions[i + 1].Id == finalizeAwait;

                if (e.Id == beginAwait)
                    inAwaitBlock = true;

                if ((inAwaitBlock || nextIsAwait) && !asyncActions.Contains(e.Actions[i].Id))
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = "Action is not asynchronous, it can not be awaited. " + (nextIsAwait ? "Next 'wait for previous' will not do anything." : "Being in await block does nothing."),
                        Line = e.Actions[i].LineId
                    };
            }

            return null;
        }
    }
}