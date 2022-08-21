using System.Collections.Generic;
using WDE.EventAiEditor.Models;

namespace WDE.EventAiEditor.Inspections
{
    public interface IScriptInspection
    {
        IEnumerable<InspectionResult> Inspect(EventAiBase script);
    }

    public interface IScriptInspectionFix
    {
        void Fix(EventAiBase script);
    }
}