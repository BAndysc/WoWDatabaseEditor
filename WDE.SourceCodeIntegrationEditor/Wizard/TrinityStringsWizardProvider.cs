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
    public class TrinityStringsWizardProvider : IWizardProvider
    {
        private readonly IContainerProvider containerRegistry;
        public string Name { get; } = "Trinity strings wizard";
        public ImageUri Image { get; } = new ImageUri("Icons/document_trinity_strings_big.png");

        public TrinityStringsWizardProvider(IContainerProvider containerRegistry)
        {
            this.containerRegistry = containerRegistry;
        }

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public Task<IWizard> Create()
        {
            return Task.FromResult<IWizard>(containerRegistry.Resolve<TrinityStringsViewModel>());
        }
    }
}