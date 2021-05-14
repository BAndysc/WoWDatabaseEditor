using System.Collections.Generic;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Inspections
{
    public interface IScriptInspection
    {
        IEnumerable<InspectionResult> Inspect(SmartScript script);
    }

    public interface IScriptInspectionFix
    {
        void Fix(SmartScript script);
    }
}