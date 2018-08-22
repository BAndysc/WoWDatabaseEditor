using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Solution
{
    public interface ISolutionItemSqlProvider<T> where T : ISolutionItem
    {
        string GenerateSql(T item);
    }
}
