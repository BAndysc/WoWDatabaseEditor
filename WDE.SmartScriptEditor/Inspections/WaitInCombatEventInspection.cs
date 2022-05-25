using System.Collections.Generic;
using System.Linq;
using WDE.Common.Managers;
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
            foreach (var a in smartDataManager.GetAllData(SmartType.SmartAction))
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
                            Line = e.LineId,
                            Severity = DiagnosticSeverity.Error,
                            Message = "WAIT action can't work in On Evade and On Kill unless you put action 'Disable AI Reset State'"
                        };
                    }
                }
            }
        }
    }
}