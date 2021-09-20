using System;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Managers;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.PacketViewer.Utils;

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
            var file = await windowManager.ShowOpenFileDialog("Sniff file|pkt,bin|Parsed packets (*.dat)|dat|All files|*");
            if (file == null)
                return null;
            return new PacketDocumentSolutionItem(file);
        }
    }
    
    [AutoRegister]
    public class PacketDocumentSolutionItemWithCustomVersionProvider : ISolutionItemProvider
    {
        private readonly IWindowManager windowManager;
        private readonly IItemFromListProvider itemFromListProvider;

        public PacketDocumentSolutionItemWithCustomVersionProvider(IWindowManager windowManager, IItemFromListProvider itemFromListProvider)
        {
            this.windowManager = windowManager;
            this.itemFromListProvider = itemFromListProvider;
        }
    
        public string GetName() => "WoW Sniff (custom build)";

        public ImageUri GetImage() => new ImageUri("Icons/document_sniff_wow_big.png");

        public string GetDescription() => "WoW packets sniffed using sniffer for analysis with custom wow version selection";

        public string GetGroupName() => "Sniffs";

        public bool IsCompatibleWithCore(ICoreVersion core) => true;

        public async Task<ISolutionItem?> CreateSolutionItem()
        {
            var file = await windowManager.ShowOpenFileDialog("Sniff file|pkt,bin|Parsed packets (*.dat)|dat|All files|*");
            if (file == null)
                return null;
            var items = Enum
                .GetValues<ClientVersionBuild>()
                .ToDictionary(v => (long)v, v => new SelectOption(v.ToString().Replace("V_", "").Replace("_", ".")));
            var version = await itemFromListProvider.GetItemFromList(items, false);
            if (version == null)
                return null;
            return new PacketDocumentSolutionItem(file, (int)version.Value);
        }
    }
}