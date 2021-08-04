using WDE.Blueprints.Data;
using WDE.Module.Attributes;

namespace WDE.Blueprints.Managers
{
    [AutoRegister]
    public class BlueprintDefinitionsRegistry : IBlueprintDefinitionsRegistry
    {
        public BlueprintDefinitionsRegistry(IBlueprintDataProvider provider)
        {
            Nodes = JsonConvert.DeserializeObject<IList<NodeDefinition>>(provider.GetBlueprintDataJson());
        }

        public IList<NodeDefinition> Nodes { get; set; }

        public IList<NodeDefinition> GetAllDefinitions()
        {
            return Nodes;
        }
    }
}