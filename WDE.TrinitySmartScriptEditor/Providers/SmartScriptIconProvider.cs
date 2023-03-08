using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScope]
    public class SmartScriptIconProvider : SmartScriptIconBaseProvider<SmartScriptSolutionItem>
    {
        public SmartScriptIconProvider(IDatabaseProvider databaseProvider, ICurrentCoreVersion currentCoreVersion) : base(databaseProvider, currentCoreVersion)
        {
        }
    }
}