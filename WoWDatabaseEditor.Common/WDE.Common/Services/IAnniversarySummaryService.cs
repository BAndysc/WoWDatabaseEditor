using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface IAnniversarySummaryService
{
    void OpenSummary();
    void TryOpenDefaultSummary();
    bool ShowAnniversaryBox { get; }
    void HideAnniversaryBox();
}