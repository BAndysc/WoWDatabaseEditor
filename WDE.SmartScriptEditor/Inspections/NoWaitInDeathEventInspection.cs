using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class NoWaitInDeathEventInspection : IEventInspection
    {
        private int waitActionId = -1;
            
        public NoWaitInDeathEventInspection(ISmartDataManager smartDataManager)
        {
            smartDataManager.GetAllData(SmartType.SmartAction).SubscribeAction(Load);
        }

        private void Load(IReadOnlyList<SmartGenericJsonData> list)
        {
            waitActionId = -1;
            foreach (var a in list)
            {
                if (a.Flags.HasFlagFast(ActionFlags.WaitAction))
                    waitActionId = a.Id;
            }
        }

        public InspectionResult? Inspect(SmartEvent e)
        {
            foreach (var a in e.Actions)
            {
                if (a.Id == waitActionId)
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Error,
                        Line = a.VirtualLineId,
                        Message = "You can not use 'WAIT' in On Death event. When the creature dies, it is no longer updated, so no action can be played with a delay"
                    };
            }

            return null;
        }
    }
}