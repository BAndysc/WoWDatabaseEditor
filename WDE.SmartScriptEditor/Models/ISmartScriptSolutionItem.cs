using System.Collections.Generic;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Models
{
    public interface ISmartScriptSolutionItem : ISolutionItem
    {
        uint? Entry { get; }
        int EntryOrGuid { get; }
        SmartScriptType SmartType { get; }
        void UpdateDependants(HashSet<long> usedTimed);
    }
}