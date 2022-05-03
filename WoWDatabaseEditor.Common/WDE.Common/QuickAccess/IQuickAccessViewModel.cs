using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

[UniqueProvider]
public interface IQuickAccessViewModel
{
    void OpenSearch(string? text);
    void CloseSearch();
    bool IsOpened { get; }
}