using WDE.Common.Solution;
using WDE.Module.Attributes;

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