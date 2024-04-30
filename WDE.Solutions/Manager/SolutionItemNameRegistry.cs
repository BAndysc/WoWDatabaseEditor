using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemNameRegistry : ISolutionItemNameRegistry
    {
        private readonly Dictionary<Type, object> nameProviders = new();
        private readonly Dictionary<Type, object> asyncNameProviders = new();

        public SolutionItemNameRegistry(IEnumerable<ISolutionNameProvider> providers,
            IEnumerable<ISolutionNameProviderAsync> asyncProviders)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionNameProvider provider in providers)
                Register((dynamic) provider);

            foreach (ISolutionNameProviderAsync provider in asyncProviders)
                RegisterAsync((dynamic)provider);
        }

        public string GetName(ISolutionItem item)
        {
            return GetName((dynamic) item);
        }

        public Task<string> GetNameAsync(ISolutionItem item)
        {
            return GetNameAsync((dynamic) item);
        }

        private void Register<T>(ISolutionNameProvider<T> provider) where T : ISolutionItem
        {
            nameProviders.Add(typeof(T), provider);
        }

        private void RegisterAsync<T>(ISolutionNameProviderAsync<T> provider) where T : ISolutionItem
        {
            asyncNameProviders.Add(typeof(T), provider);
        }

        private string GetName<T>(T item) where T : ISolutionItem
        {
            if (nameProviders.TryGetValue(item.GetType(), out var nameProvider))
            {
                var x = (ISolutionNameProvider<T>)nameProvider;
                return x.GetName(item);
            }
#if DEBUG
            return item.GetType().Name + " (missing ISolutionNameProvider<T>)";
#else
            return item.GetType().Name;
#endif
        }

        private async Task<string> GetNameAsync<T>(T item) where T : ISolutionItem
        {
            if (asyncNameProviders.TryGetValue(item.GetType(), out var nameProvider))
            {
                var x = (ISolutionNameProviderAsync<T>)nameProvider;
                return await x.GetNameAsync(item);
            }

            return GetName<T>(item);
        }
    }
}