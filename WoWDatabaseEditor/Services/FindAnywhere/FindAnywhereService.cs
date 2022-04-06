using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
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

    public FindAnywhereService(Func<IFindAnywhereResultsViewModel> resultsCreator,
        Lazy<IDocumentManager> documentManager,
        IEnumerable<IFindAnywhereSource> sources)
    {
        this.resultsCreator = resultsCreator;
        this.documentManager = documentManager;
        this.sources = sources.OrderBy(x => x.Order).ToList();
    }
    
    public void OpenFind(IReadOnlyList<string> parameter, long value)
    {
        var results = resultsCreator();
        results.Search(parameter, value).ListenErrors();
        documentManager.Value.OpenDocument(results);
    }

    public async Task Find(IFindAnywhereResultContext resultContext, IReadOnlyList<string> parameterName, long parameterValue, CancellationToken cancellationToken)
    {
        foreach (var source in sources)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            
            await source.Find(resultContext, parameterName, parameterValue, cancellationToken);
        }
    }
}