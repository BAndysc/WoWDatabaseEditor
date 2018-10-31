using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.Common.Solution;

namespace WDE.Blueprints.Providers
{
    [AutoRegister]
    public class BlueprintNameProvider : ISolutionNameProvider<BlueprintSolutionItem>
    {
        public string GetName(BlueprintSolutionItem item)
        {
            return "Blueprint";
        }
    }
}
