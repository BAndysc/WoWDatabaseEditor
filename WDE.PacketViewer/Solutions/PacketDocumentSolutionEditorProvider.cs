using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.PacketViewer.ViewModels;

namespace WDE.PacketViewer.Solutions
{
    [AutoRegister]
    public class PacketDocumentSolutionEditorProvider : ISolutionItemEditorProvider<PacketDocumentSolutionItem>
    {
        private readonly IContainerProvider containerProvider;

        public PacketDocumentSolutionEditorProvider(IContainerProvider containerProvider)
        {
            this.containerProvider = containerProvider;
        }
        
        public IDocument GetEditor(PacketDocumentSolutionItem item)
        {
            return containerProvider.Resolve<PacketDocumentViewModel>((typeof(PacketDocumentSolutionItem), item));
        }
    }
}