using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.FindAnywhere;

[NonUniqueProvider]
public interface IFindAnywhereSource
{
    int Order => 0;
    FindAnywhereSourceType SourceType { get; }
    Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType searchType, IReadOnlyList<string> parameterNames, long parameterValue, CancellationToken cancellationToken);
}