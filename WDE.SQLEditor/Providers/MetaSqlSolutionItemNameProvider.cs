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