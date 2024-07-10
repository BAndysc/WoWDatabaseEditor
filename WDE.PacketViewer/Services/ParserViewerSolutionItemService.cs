using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.PacketViewer.Solutions;

namespace WDE.PacketViewer.Services;

[AutoRegister]
[SingleInstance]
public class ParserViewerSolutionItemService : IParserViewerSolutionItemService
{
    public bool Enabled => true;

    public ISolutionItem CreateSolutionItem(string? file, int? version = null, bool liveStream = false)
    {
        return new PacketDocumentSolutionItem(file, version, liveStream);
    }
}