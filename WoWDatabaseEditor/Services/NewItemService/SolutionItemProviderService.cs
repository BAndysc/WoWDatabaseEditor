using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    [AutoRegister]
    public class SolutionItemProviderService : ISolutionItemProvideService
    {
        public IEnumerable<ISolutionItemProvider> AllCompatible { get; }

        public SolutionItemProviderService(IEnumerable<ISolutionItemProvider> items, 
            IEnumerable<ISolutionItemProviderProvider> providers,
            ICurrentCoreVersion coreVersion)
        {
            AllCompatible = items
                .Concat(providers.SelectMany(p => p.Provide()))
                .Where(i => i.IsCompatibleWithCore(coreVersion.Current))
                .ToList();
        }
    }
}