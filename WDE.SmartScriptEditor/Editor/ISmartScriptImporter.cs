using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptImporter
    {
        Task Import(SmartScript script, bool doNotTouchIfPossible, IList<ISmartScriptLine> lines, IList<IConditionLine> conditions, IList<IConditionLine>? targetConditions);
    }
}