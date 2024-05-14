using System;
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
        public ISmartScriptProjectItem? Serialize(PacketDocumentSolutionItem item, bool forMostRecentlyUsed)
        {
            if (item.LiveStream)
                return null;

            return new AbstractSmartScriptProjectItem()
            {
                Type = 25,
                StringValue = item.CustomVersion.HasValue ? $"{item.CustomVersion.Value}|{item.File}" : item.File
            };
        }

        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            if (projectItem.Type == 25 && !string.IsNullOrEmpty(projectItem.StringValue))
            {
                var indexOfPipe = projectItem.StringValue.IndexOf("|", StringComparison.Ordinal);
                var path = projectItem.StringValue;
                int? version = null;
                if (indexOfPipe != -1)
                {
                    if (int.TryParse(path.Substring(0, indexOfPipe), out var version_))
                    {
                        version = version_;
                        path = path.Substring(indexOfPipe + 1);
                    }
                }
                solutionItem = new PacketDocumentSolutionItem(path, version);
                return true;
            }
            solutionItem = null;
            return false;
        }
    }
}