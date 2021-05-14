using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public class ParameterRangeInspection : IActionInspection, IEventInspection
    {
        private static bool ContainsAllFlags(FlagParameter fp, long val)
        {
            if (fp.Items == null)
                return true;
            
            for (int i = 0; i < 32; ++i)
            {
                if (((1 << i) & val) > 0 && !fp.Items.ContainsKey(1 << i))
                    return false;
            }
            return true;
        }
        
        public InspectionResult? Inspect(SmartBaseElement e)
        {
            for (int i = 0; i < e.ParametersCount; ++i)
            {
                if (e.GetParameter(i).IsUsed &&
                    e.GetParameter(i).Value != 0 && 
                    e.GetParameter(i).HasItems && 
                    !((e.GetParameter(i).Parameter.Items?.ContainsKey(e.GetParameter(i).Value) ?? false) ||
                      (e.GetParameter(i).Parameter is FlagParameter fp && ContainsAllFlags(fp, e.GetParameter(i).Value))))
                {
                    return new InspectionResult()
                    {
                        Severity = DiagnosticSeverity.Info,
                        Message = $"Parameter `{e.GetParameter(i).Name}` value out of range ({e.GetParameter(i).Value})",
                        Line = e.LineId
                    };
                }
            }

            return null;
        }

        public InspectionResult? Inspect(SmartAction a)
        {
            return Inspect((SmartBaseElement)a);
        }

        public InspectionResult? Inspect(SmartEvent e)
        {
            return Inspect((SmartBaseElement)e);
        }
    }
}