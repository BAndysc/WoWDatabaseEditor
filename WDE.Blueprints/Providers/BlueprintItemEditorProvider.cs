using System;
using WDE.Blueprints.Editor.ViewModels;
using WDE.Blueprints.Editor.Views;
using WDE.Blueprints.Managers;
using WDE.Module.Attributes;
using WDE.Common.Managers;
using WDE.Common.Solution;

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
