using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.FindAnywhere;

[UniqueProvider]
public interface IFindAnywhereService
{
    void OpenFind(IReadOnlyList<string> parameterName, long value);
    void OpenFind(IReadOnlyList<string> parameterName, IReadOnlyList<long> values);
    Task Find(IFindAnywhereResultContext resultContext, FindAnywhereSourceType sourceTypes, IReadOnlyList<string> parameterName, IReadOnlyList<long> parameterValue, CancellationToken cancellationToken);
}