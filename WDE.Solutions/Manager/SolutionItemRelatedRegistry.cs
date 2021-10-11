using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemRelatedRegistry : ISolutionItemRelatedRegistry
    {
        private readonly Dictionary<Type, object> relatedProviders = new();

        public SolutionItemRelatedRegistry(IEnumerable<ISolutionItemRelatedProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemRelatedProvider provider in providers)
                Register((dynamic) provider);
        }

        private void Register<T>(ISolutionItemRelatedProvider<T> provider) where T : ISolutionItem
        {
            relatedProviders.Add(typeof(T), provider);
        }

        private Task<RelatedSolutionItem?> GetRelated<T>(T item) where T : ISolutionItem
        {
            if (relatedProviders.TryGetValue(item.GetType(), out var nameProvider))
            {
                var x = (ISolutionItemRelatedProvider<T>)nameProvider;
                return x.GetRelated(item);
            }

            return Task.FromResult<RelatedSolutionItem?>(null);
        }

        public Task<RelatedSolutionItem?> GetRelated(ISolutionItem item)
        {
            return GetRelated((dynamic) item);
        }
    }
}