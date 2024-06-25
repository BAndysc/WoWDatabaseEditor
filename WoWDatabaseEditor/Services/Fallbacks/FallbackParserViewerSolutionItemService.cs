using WDE.Common;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.Fallbacks;

[FallbackAutoRegister]
[SingleInstance]
public class FallbackParserViewerSolutionItemService : IParserViewerSolutionItemService
{
    public bool Enabled => false;

    public ISolutionItem CreateSolutionItem(string? file, int? version = null, bool liveStream = false)
    {
        throw new System.NotImplementedException();
    }
}