using WDE.Module.Attributes;

namespace WDE.Blueprints.Managers
{
    [AutoRegister]
    public class BlueprintDataProvider : IBlueprintDataProvider
    {
        public string GetBlueprintDataJson()
        {
            return File.ReadAllText("BlueprintData/nodes.json");
        }
    }
}