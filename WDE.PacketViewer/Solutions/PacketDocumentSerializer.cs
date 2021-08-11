using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Solutions
{
    [AutoRegister]
    public class PacketDocumentSerializer : ISolutionItemSerializer<PacketDocumentSolutionItem>,
        ISolutionItemDeserializer<PacketDocumentSolutionItem>
    {
        public ISmartScriptProjectItem Serialize(PacketDocumentSolutionItem item)
        {
            return new AbstractSmartScriptProjectItem()
            {
                Type = 25,
                StringValue = item.File
            };
        }

        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            if (projectItem.Type == 25 && !string.IsNullOrEmpty(projectItem.StringValue))
            {
                solutionItem = new PacketDocumentSolutionItem(projectItem.StringValue);
                return true;
            }
            solutionItem = null;
            return false;
        }
    }
}