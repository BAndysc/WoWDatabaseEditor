using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Solution
{
    public interface ISolutionItemSqlGeneratorRegistry
    {
        void Register<T>(ISolutionItemSqlProvider<T> provider) where T : ISolutionItem;
        string GenerateSql<T>(T item) where T : ISolutionItem;
    }
}
