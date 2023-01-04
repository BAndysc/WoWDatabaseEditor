using System.Collections.Generic;
using System.Linq;
using WDE.Common.Managers;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class WaitInCombatEventInspection : IScriptInspection
    {
        private int waitActionId = -1;
        private int disableResetAi = -1;
            
        public WaitInCombatEventInspection(ISmartDataManager smartDataManager)
        {
            smartDataManager.GetAllData(SmartType.SmartAction).SubscribeAction(Load);
        }

        private void Load(IReadOnlyList<SmartGenericJsonData> list)
        {
            waitActionId = -1;
            disableResetAi = -1;
            foreach (var a in list)
            {
                if (a.Flags.HasFlagFast(ActionFlags.WaitAction))
                    waitActionId = a.Id;
                else if (a.NameReadable == "Disable reset AI state")
                    disableResetAi = a.Id;
            }
        }

        public IEnumerable<InspectionResult> Inspect(SmartScriptBase script)
        {
            var anyDisableReset = script.AllActions.Any(a => a.Id == disableResetAi);
            if (anyDisableReset)
                yield break;

            foreach (var e in script.Events)
            {
                if (e.Id == 5 || e.Id == 7)
                {
                    var anyWait = e.Actions.Any(a => a.Id == waitActionId);
                    if (anyWait)
                    {
                        yield return new InspectionResult()
                        {
                            Line = e.VirtualLineId,
                            Severity = DiagnosticSeverity.Error,
                            Message = "WAIT action can't work in On Evade and On Kill unless you put action 'Disable AI Reset State'"
                        };
                    }
                }
            }
        }
    }
}