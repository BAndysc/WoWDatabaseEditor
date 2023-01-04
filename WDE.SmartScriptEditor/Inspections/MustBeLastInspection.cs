using System;
using System.Collections.Generic;
using WDE.Common.Managers;
using WDE.Common.Utils;
using WDE.MVVM.Observable;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections;

public class MustBeLastInspection : IEventInspection
{
    private int action = -1;
            
    public MustBeLastInspection(ISmartDataManager smartDataManager)
    {
        smartDataManager.GetAllData(SmartType.SmartAction).SubscribeAction(Load);
    }

    private void Load(IReadOnlyList<SmartGenericJsonData> list)
    {
        action = -1;
        foreach (var a in list)
        {
            if (a.Flags.HasFlagFast(ActionFlags.MustBeLast))
            {
                if (action != -1)
                    throw new Exception("Multiple must be last actions found, change it to HashSet then");
                action = a.Id;
            }
        }
    }

    public InspectionResult? Inspect(SmartEvent e)
    {
        // -1, because we skip the last action
        for (int i = 0; i < e.Actions.Count - 1; ++i)
        {
            if (e.Actions[i].Id == action)
                return new InspectionResult()
                {
                    Severity = DiagnosticSeverity.Error,
                    Message = $"Action `{e.Actions[i].Readable.RemoveTags()}` must be the last action",
                    Line = e.Actions[i].VirtualLineId
                };
        }
        
        return null;
    }
}