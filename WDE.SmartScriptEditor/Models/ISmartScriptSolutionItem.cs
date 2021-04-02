using WDE.Common;
using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Models
{
    public interface ISmartScriptSolutionItem : ISolutionItem
    {
        int Entry { get; }
        SmartScriptType SmartType { get; }
    }
}