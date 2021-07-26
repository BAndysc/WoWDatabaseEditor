using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Solutions
{
    [AutoRegister]
    public class PacketDocumentSolutionItemProvider : ISolutionItemProvider
    {
        private readonly IWindowManager windowManager;

        public PacketDocumentSolutionItemProvider(IWindowManager windowManager)
        {
            this.windowManager = windowManager;
        }
    
        public string GetName() => "WoW Sniff";

        public ImageUri GetImage() => new ImageUri("Icons/document_sniff_big.png");

        public string GetDescription() => "WoW packets sniffed using sniffer for analysis";

        public string GetGroupName() => "Sniffs";

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var file = await windowManager.ShowOpenFileDialog("Sniff file|pkt|Parsed packets (*.dat)|dat|All files|*");
            if (file == null)
                return null;
            return new PacketDocumentSolutionItem(file);
        }
    }
}