using WDE.Common.Database;
using WDE.Common.DBC;
using WDE.Common.Parameters;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Providers;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptNameProvider : SmartScriptNameProviderBase<SmartScriptSolutionItem>
    {
        public SmartScriptNameProvider(ICachedDatabaseProvider database, ISpellStore spellStore, IDbcStore dbcStore,
            IParameterFactory parameterFactory) : base(database, spellStore, dbcStore, parameterFactory)
        {
        }
        
        public override string GetName(SmartScriptSolutionItem item)
        {
            var name = base.GetName(item);
            if (item.SmartType == SmartScriptType.Creature ||
                item.SmartType == SmartScriptType.GameObject ||
                item.SmartType == SmartScriptType.TimedActionList ||
                item.SmartType == SmartScriptType.AreaTrigger)
                return name + " smart ai";
            return name;
        }
    }
}