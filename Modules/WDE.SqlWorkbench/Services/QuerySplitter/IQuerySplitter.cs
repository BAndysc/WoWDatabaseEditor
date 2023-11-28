using System.Collections.Generic;
using System.Threading.Tasks;
using AvaloniaEdit.Document;

namespace WDE.SqlWorkbench.Services.QuerySplitter;

internal interface IQuerySplitter
{
    bool AreRangesValid { get; }
    void UpdateRangesNow(ITextSource source);
    Task<bool> UpdateRangesAsync(ITextSource source);
    StatementRange? GetRange(int caretOffset);
    void InvalidateRanges();
    IReadOnlyList<StatementRange> Ranges { get; }
}