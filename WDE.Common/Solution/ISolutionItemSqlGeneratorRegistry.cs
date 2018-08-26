using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDE.Common.Solution
{
    public interface ISolutionItemSqlGeneratorRegistry
    {
        string GenerateSql(ISolutionItem item);
    }
}
