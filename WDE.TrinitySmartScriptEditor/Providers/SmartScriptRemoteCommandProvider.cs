using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.Conditions;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScope]
    public class SmartScriptRemoteCommandProvider : ISolutionItemRemoteCommandProvider<SmartScriptSolutionItem>
    {
        public IRemoteCommand[] GenerateCommand(SmartScriptSolutionItem item) =>
            new IRemoteCommand[]
            {
                new CreatureTemplateReloadRemoteCommand((uint) item.EntryOrGuid), 
                SmartScriptReloadRemoteCommand.Singleton,
                ConditionsReloadRemoteCommand.Singleton
            };
    }
}