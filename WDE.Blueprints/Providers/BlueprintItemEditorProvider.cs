using WDE.Blueprints.Editor.ViewModels;
using WDE.Blueprints.Managers;
using WDE.Common.Managers;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.Blueprints.Providers
{
    [AutoRegister]
    public class BlueprintItemEditorProvider : ISolutionItemEditorProvider<BlueprintSolutionItem>
    {
        private readonly IBlueprintDefinitionsRegistry blueprintDefinitionsRegistry;

        public BlueprintItemEditorProvider(IBlueprintDefinitionsRegistry blueprintDefinitionsRegistry)
        {
            this.blueprintDefinitionsRegistry = blueprintDefinitionsRegistry;
        }

        public IDocument GetEditor(BlueprintSolutionItem item)
        {
            return new BlueprintEditorViewModel(item, new NodesViewModel(blueprintDefinitionsRegistry));
        }
    }
}