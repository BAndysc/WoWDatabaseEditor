using System.Collections.Generic;
using System.Linq;
using WDE.Common;
using WDE.Common.CoreVersion;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.NewItemService
{
    [AutoRegister]
    public class SolutionItemProviderService : ISolutionItemProvideService
    {
        private List<IRelatedSolutionItemCreator> relatedCreators { get; }
        public IEnumerable<ISolutionItemProvider> AllCompatible { get; }
        
        public IEnumerable<IRelatedSolutionItemCreator> GetRelatedCreators(RelatedSolutionItem related)
        {
            foreach (var creator in relatedCreators)
                if (creator.CanCreatedRelatedSolutionItem(related))
                    yield return creator;
        }

        public SolutionItemProviderService(IEnumerable<ISolutionItemProvider> items, 
            IEnumerable<ISolutionItemProviderProvider> providers,
            ICurrentCoreVersion coreVersion)
        {
            AllCompatible = items
                .Concat(providers.SelectMany(p => p.Provide()))
                .Where(i => i.IsCompatibleWithCore(coreVersion.Current))
                .ToList();

            relatedCreators = AllCompatible
                .Where(c => c is IRelatedSolutionItemCreator)
                .Cast<IRelatedSolutionItemCreator>()
                .ToList();
        }
    }
}