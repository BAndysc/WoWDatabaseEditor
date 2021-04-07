using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemSqlGeneratorRegistry : ISolutionItemSqlGeneratorRegistry
    {
        private readonly Dictionary<Type, object> sqlProviders = new();

        public SolutionItemSqlGeneratorRegistry(IEnumerable<ISolutionItemSqlProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (ISolutionItemSqlProvider provider in providers)
                Register((dynamic) provider);
        }

        public Task<string> GenerateSql(ISolutionItem item)
        {
            return GenerateSql((dynamic) item);
        }

        private void Register<T>(ISolutionItemSqlProvider<T> provider) where T : ISolutionItem
        {
            sqlProviders.Add(typeof(T), provider);
        }

        private async Task<string> GenerateSql<T>(T item) where T : ISolutionItem
        {
            if (sqlProviders.TryGetValue(item.GetType(), out var provider))
            {
                var x = provider as ISolutionItemSqlProvider<T>;
                return await x.GenerateSql(item);
            }
            else
            {
                return
                    $"--- INTERNAL WoW Database Editor ERROR ---\n\n{item.GetType()} unknown SQL generator. Development info: You need to register class implementing ISolutionItemSqlProvider<T> interface";
            }
        }
    }
}