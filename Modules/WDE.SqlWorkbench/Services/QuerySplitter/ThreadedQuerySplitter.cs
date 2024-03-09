using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using Microsoft.Extensions.ObjectPool;

namespace WDE.SqlWorkbench.Services.QuerySplitter;

internal class ThreadedQuerySplitter : IQuerySplitter
{
    private List<StatementRange> ranges = new();
    private CancellationTokenSource? pendingUpdate;
    private ObjectPool<List<StatementRange>> pool = new DefaultObjectPool<List<StatementRange>>(new DefaultPooledObjectPolicy<List<StatementRange>>());

    public bool AreRangesValid { get; private set; }
    
    public void UpdateRangesNow(ITextSource source)
    {
        ranges.Clear();
        MySQLParserServicesImpl.DetermineStatementRanges(source, ranges, CancellationToken.None);
        AreRangesValid = true;
    }

    public async Task<bool> UpdateRangesAsync(ITextSource source)
    {
        AreRangesValid = false;
        if (pendingUpdate != null)
            await pendingUpdate.CancelAsync();
        var cts = pendingUpdate = new CancellationTokenSource();

        List<StatementRange>? result = null;
        try
        {
            result = await Task.Run(() =>
            {
                var list = pool.Get();
                MySQLParserServicesImpl.DetermineStatementRanges(source, list, cts.Token);
                return list;
            }, cts.Token);
            
            if (!cts.IsCancellationRequested)
            {
                Debug.Assert(cts == pendingUpdate);
                pendingUpdate = null;
                ranges.Clear();
                ranges.AddRange(result);
                AreRangesValid = true;
                return true;
            }
        }
        finally
        {
            if (result != null)
            {
                result.Clear();
                pool.Return(result);
            }
        }

        return false;
    }

    public StatementRange? GetRange(int caretOffset)
    {
        if (!AreRangesValid)
            return null;
        
        // binary search in ranges
        var index = ranges.BinarySearch(new StatementRange(caretOffset, 0, 0));
        if (index >= 0)
        {
            return ranges[index];
        }
        else
        {
            index = ~index;
            if (index == 0)
                return null;
            
            var range = ranges[index - 1];
            return range;
        }
    }

    public void InvalidateRanges()
    {
        AreRangesValid = false;
    }

    public IReadOnlyList<StatementRange> Ranges => ranges;
}