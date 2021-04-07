using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Editor.ViewModels;
using WDE.SmartScriptEditor.Models;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScopeAttribute]
    public class SmartScriptEditorProvider : ISolutionItemEditorProvider<SmartScriptSolutionItem>
    {
        private readonly IContainerProvider containerProvider;

        public SmartScriptEditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }

        public IDocument GetEditor(SmartScriptSolutionItem item)
        {
            SmartScriptEditorViewModel vm = containerProvider.Resolve<SmartScriptEditorViewModel>((typeof(ISmartScriptSolutionItem), item));
            return vm;
        }
    }
}