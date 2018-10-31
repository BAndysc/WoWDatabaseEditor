using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Solution
{
    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider
    {

    }

    [NonUniqueProvider]
    public interface ISolutionItemSqlProvider<T> : ISolutionItemSqlProvider where T : ISolutionItem
    {
        string GenerateSql(T item);
    }
}
