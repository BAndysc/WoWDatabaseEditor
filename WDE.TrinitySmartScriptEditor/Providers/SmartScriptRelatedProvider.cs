using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Providers;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScope]
    public class SmartScriptRelatedProvider : SmartScriptRelatedProviderBase<SmartScriptSolutionItem>
    {
        public SmartScriptRelatedProvider(IDatabaseProvider databaseProvider) : base(databaseProvider)
        {
        }
    }
}