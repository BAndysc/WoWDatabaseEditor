using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor.Editor.ViewModels;

namespace WDE.SmartScriptEditor.Providers
{
    [AutoRegister]
    public class SmartScriptEditorProvider : ISolutionItemEditorProvider<SmartScriptSolutionItem>
    {
        private readonly IContainerProvider containerProvider;
        private readonly ISolutionItemNameRegistry solutionItemNameRegistry;

        public SmartScriptEditorProvider(ISolutionItemNameRegistry solutionItemNameRegistry,
            IContainerProvider containerProvider)
        {
            this.solutionItemNameRegistry = solutionItemNameRegistry;
            this.containerProvider = containerProvider;
        }

        public IDocument GetEditor(SmartScriptSolutionItem item)
        {
            var vm = containerProvider.Resolve<SmartScriptEditorViewModel>();
            vm.SetSolutionItem(item);

            return vm;
        }
    }
}