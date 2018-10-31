using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSolutionSqlNameProvider : ISolutionNameProvider<MetaSolutionSQL>
    {
        public string GetName(MetaSolutionSQL item)
        {
            return "sql";
        }
    }
}
