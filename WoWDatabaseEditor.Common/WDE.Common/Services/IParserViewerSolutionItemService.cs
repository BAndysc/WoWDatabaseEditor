using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IParserViewerSolutionItemService
{
    bool Enabled { get; }
    ISolutionItem CreateSolutionItem(string? file, int? version = null, bool liveStream = false);
}