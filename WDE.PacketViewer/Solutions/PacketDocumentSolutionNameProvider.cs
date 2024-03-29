using System.IO;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.PacketViewer.Solutions
{
    [AutoRegister]
    public class PacketDocumentSolutionNameProvider : ISolutionNameProvider<PacketDocumentSolutionItem>, ISolutionItemIconProvider<PacketDocumentSolutionItem>
    {
        public string GetName(PacketDocumentSolutionItem item)
        {
            if (item.CustomVersion.HasValue)
                return $"Sniff {Path.GetFileNameWithoutExtension(item.File)} ({item.CustomVersion.Value})";
            return "Sniff " + Path.GetFileNameWithoutExtension(item.File);
        }

        public ImageUri GetIcon(PacketDocumentSolutionItem icon)
        {
            return new ImageUri("Icons/document_sniff.png");
        }
    }
}