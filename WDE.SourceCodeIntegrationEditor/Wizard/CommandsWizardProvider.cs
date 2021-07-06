using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.CoreVersion;
using WDE.Common.Documents;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.SourceCodeIntegrationEditor.ViewModels;

namespace WDE.SourceCodeIntegrationEditor.Wizard
{
    [SingleInstance]
    [AutoRegister]
    public class CommandsWizardProvider : IWizardProvider
    {
        private readonly IContainerProvider containerRegistry;
        public string Name { get; } = "Commands wizard";
        public ImageUri Image { get; } = new ImageUri("Icons/document_rbac.png");
        
        public CommandsWizardProvider(IContainerProvider containerRegistry)
        {
            this.containerRegistry = containerRegistry;
        }

        public bool IsCompatibleWithCore(ICoreVersion core)
        {
            return core.SupportsRbac;
        }

        public Task<IWizard> Create()
        {
            return Task.FromResult<IWizard>(containerRegistry.Resolve<CommandsDocumentViewModel>());
        }
    }
}