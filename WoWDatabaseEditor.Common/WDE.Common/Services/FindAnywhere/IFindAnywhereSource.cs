using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services.FindAnywhere;

[NonUniqueProvider]
public interface IFindAnywhereSource
{
    int Order => 0;
    Task Find(IFindAnywhereResultContext resultContext, IReadOnlyList<string> parameterName, long parameterValue, CancellationToken cancellationToken);
}