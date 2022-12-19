using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Managers;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.FindAnywhere;

[UniqueProvider]
[AutoRegister]
public class FindAnywhereService : IFindAnywhereService
{
    private readonly Func<IFindAnywhereResultsViewModel> resultsCreator;
    private readonly Lazy<IDocumentManager> documentManager;
    private readonly IList<IFindAnywhereSource> sources;
    private readonly LRUCache<(string, long, FindAnywhereSourceType), ToListFindAnywhereResultContext> cache = new(30);

    public FindAnywhereService(Func<IFindAnywhereResultsViewModel> resultsCreator,
        Lazy<IDocumentManager> documentManager,
        IEnumerable<IFindAnywhereSource> sources)
    {
        this.resultsCreator = resultsCreator;
        this.documentManager = documentManager;
        this.sources = sources.OrderBy(x => x.Order).ToList();
    }
    
    public void OpenFind(IReadOnlyList<string> parameter, long value) => OpenFind(parameter, new List<long>(){value});

    public void OpenFind(IReadOnlyList<string> parameter, IReadOnlyList<long> values)
    {
        var results = resultsCreator();
        results.Search(parameter, values).ListenErrors();
        documentManager.Value.OpenDocument(results);
    }

    public async Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType sourceTypes, IReadOnlyList<string> parameterName, IReadOnlyList<long> parameterValue, CancellationToken cancellationToken)
    {
        // cacheable version
        if (parameterName.Count == 1 && parameterValue.Count == 1)
        {
            if (cache.TryGetValue((parameterName[0], parameterValue[0], sourceTypes), out var results) && results is {})
            {
                foreach (var result in results.Results)
                    resultContext.AddResult(result);
            }
            else
            {
                var listResults = new ToListFindAnywhereResultContext();
                var multiplexResults = new MultiplexFindAnywhereResultContext(listResults, resultContext);
                foreach (var source in sources)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
            
                    if ((source.SourceType & sourceTypes) == 0)
                        continue;
                    
                    foreach (var val in parameterValue)
                        await source.Find(multiplexResults, sourceTypes, parameterName, val, cancellationToken);
                }
                cache[(parameterName[0], parameterValue[0], sourceTypes)] = listResults;
            }
        }
        else
        {
            foreach (var source in sources)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if ((source.SourceType & sourceTypes) == 0)
                    continue;
            
                foreach (var val in parameterValue)
                    await source.Find(resultContext, sourceTypes, parameterName, val, cancellationToken);
            }   
        }
    }
}