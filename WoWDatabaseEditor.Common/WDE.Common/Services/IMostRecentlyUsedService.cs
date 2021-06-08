using System.Collections.Generic;
using WDE.Common.Database;
using WDE.Module.Attributes;

namespace WDE.Common.Services
{
    [UniqueProvider]
    public interface IMostRecentlyUsedService
    {
        IEnumerable<ISolutionItem> MostRecentlyUsed { get; }
    }
}