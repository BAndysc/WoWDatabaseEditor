using System.Collections.Generic;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Editor
{
    public interface ISmartScriptImporter
    {
        void Import(SmartScript script, IList<ISmartScriptLine> lines, IList<IConditionLine> conditions, IList<IConditionLine> targetConditions);
    }
}