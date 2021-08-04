using WDE.Blueprints.Data;

namespace WDE.Blueprints.Managers
{
    public interface IBlueprintDefinitionsRegistry
    {
        IList<NodeDefinition> GetAllDefinitions();
    }
}