using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Attributes;
using WDE.Common.Solution;

namespace WDE.Solutions.Manager
{
    [AutoRegister]
    public class SolutionItemSqlGeneratorRegistry : ISolutionItemSqlGeneratorRegistry
    {
        private Dictionary<Type, object> sqlProviders = new Dictionary<Type, object>();

        public SolutionItemSqlGeneratorRegistry(IEnumerable<ISolutionItemSqlProvider> providers)
        {
            // handy trick with (dynamic) cast, thanks to this proper Generic method will be called!
            foreach (var provider in providers)
                Register((dynamic)provider);
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

        public string GenerateSql(ISolutionItem item)
        {
            return GenerateSql((dynamic)item);
        }
    }
}
