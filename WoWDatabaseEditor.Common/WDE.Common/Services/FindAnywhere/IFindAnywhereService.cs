using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.FindAnywhere;

[UniqueProvider]
public interface IFindAnywhereService
{
    void OpenFind(IReadOnlyList<string> parameterName, long value);
    Task Find(IFindAnywhereResultContext resultContext, IReadOnlyList<string> parameterName, long parameterValue, CancellationToken cancellationToken);
}