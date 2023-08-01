using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScope]
    public class SmartScriptIconProvider : SmartScriptIconBaseProvider<SmartScriptSolutionItem>
    {
        public SmartScriptIconProvider(ICachedDatabaseProvider databaseProvider, ICurrentCoreVersion currentCoreVersion) : base(databaseProvider, currentCoreVersion)
        {
        }
    }
}