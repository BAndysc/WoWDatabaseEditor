using System;
using System.Collections.Generic;
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

        public string GenerateSql(ISolutionItem item)
        {
            return GenerateSql((dynamic) item);
        }

        private void Register<T>(ISolutionItemSqlProvider<T> provider) where T : ISolutionItem
        {
            sqlProviders.Add(typeof(T), provider);
        }

        private string GenerateSql<T>(T item) where T : ISolutionItem
        {
            var x = sqlProviders[item.GetType()] as ISolutionItemSqlProvider<T>;
            return x.GenerateSql(item);
        }
    }
}