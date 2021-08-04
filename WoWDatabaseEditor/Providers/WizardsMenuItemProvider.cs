using System.Collections.Generic;
using System.Linq;
using Prism.Commands;
using WDE.Common.CoreVersion;
using WDE.Common.Documents;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class WizardsMenuItemProvider : IMainMenuItem
    {
        private readonly IDocumentManager documentManager;

        public WizardsMenuItemProvider(IEnumerable<IWizardProvider> wizards, 
            ICurrentCoreVersion currentCoreVersion,
            IDocumentManager documentManager)
        {
            this.documentManager = documentManager;
            foreach (var wizard in wizards.Where(w => w.IsCompatibleWithCore(currentCoreVersion.Current)))
            {
                SubItems.Add(new ModuleMenuItem(wizard.Name, new DelegateCommand(async () =>
                {
                    var document = await wizard.Create();
                    documentManager.OpenDocument(document);
                })));
            }
        }
        
        public string ItemName => "Wizards";
        public List<IMenuItem> SubItems { get; } = new List<IMenuItem>();
        public MainMenuItemSortPriority SortPriority => MainMenuItemSortPriority.PriorityNormal;
    }
}