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
        private Dictionary<Type, object> nameProviders = new Dictionary<Type, object>();

        public void Register<T>(ISolutionItemSqlProvider<T> provider) where T : ISolutionItem
        {
            nameProviders.Add(typeof(T), provider);
        }
        
        public string GenerateSql<T>(T item) where T : ISolutionItem
        {
            var x = nameProviders[item.GetType()] as ISolutionItemSqlProvider<T>;
            return x.GenerateSql(item);
        }
    }
}
