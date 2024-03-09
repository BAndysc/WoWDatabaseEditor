using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
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
            smartDataManager.GetAllData(SmartType.SmartAction).SubscribeAction(Load);
        }

        private void Load(IReadOnlyList<SmartGenericJsonData> list)
        {
            asyncActions.Clear();
            beginAwait = -1;
            finalizeAwait = -1;
            
            foreach (var a in list)
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

                if (e.Actions[i].Id == beginAwait)
                    inAwaitBlock = true;

                if ((inAwaitBlock || nextIsAwait) && !asyncActions.Contains(e.Actions[i].Id))
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Warning,
                        Message = "Action is not asynchronous, it can not be awaited. " + (nextIsAwait ? "Next 'wait for previous' will not do anything." : "Being in await block does nothing."),
                        Line = e.Actions[i].VirtualLineId
                    };
            }

            return null;
        }
    }
}