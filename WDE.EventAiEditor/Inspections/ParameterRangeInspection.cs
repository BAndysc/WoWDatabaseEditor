using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public class ParameterRangeInspection : IActionInspection, IEventInspection
    {
        private static int ContainsAllFlags(FlagParameter fp, long val)
        {
            if (fp.Items == null)
                return 0;
            
            for (int i = 0; i < 32; ++i)
            {
                if (((1 << i) & val) > 0 && !fp.Items.ContainsKey(1 << i))
                    return (1 << i);
            }
            return 0;
        }
        
        public InspectionResult? Inspect(EventAiBaseElement e)
        {
            for (int i = 0; i < e.ParametersCount; ++i)
            {
                if (e.GetParameter(i).IsUsed &&
                    e.GetParameter(i).HasItems)
                {
                    if (e.GetParameter(i).Parameter is FlagParameter fp &&
                        ContainsAllFlags(fp, e.GetParameter(i).Value) != 0)
                    {
                        var unknownFlag = ContainsAllFlags(fp, e.GetParameter(i).Value);
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Info,
                            Message = $"Parameter `{e.GetParameter(i).Name}` uses non existing flag {unknownFlag}",
                            Line = e.LineId
                        }; 
                    }
                    
                    if (e.GetParameter(i).Value != 0 &&
                        e.GetParameter(i).Parameter is not FlagParameter &&
                        !e.GetParameter(i).Parameter.AllowUnknownItems &&
                        !(e.GetParameter(i).Items?.ContainsKey(e.GetParameter(i).Value) ?? false))
                    {
                        return new InspectionResult()
                        {
                            Severity = DiagnosticSeverity.Info,
                            Message = $"Parameter `{e.GetParameter(i).Name}` value out of range ({e.GetParameter(i).Value})",
                            Line = e.LineId
                        };   
                    }
                }
            }

            return null;
        }

        public InspectionResult? Inspect(EventAiAction a)
        {
            return Inspect((EventAiBaseElement)a);
        }

        public InspectionResult? Inspect(EventAiEvent e)
        {
            return Inspect((EventAiBaseElement)e);
        }
    }
}