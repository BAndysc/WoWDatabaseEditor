using System.Collections.Generic;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Menu;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Providers
{
    [AutoRegister]
    public class EditorViewMenuItemProvider: IMainMenuItem
    {
        public string ItemName { get; } = "_View";
        public List<IMenuItem> SubItems { get; }
        public MainMenuItemSortPriority SortPriority { get; } = MainMenuItemSortPriority.PriorityHigh;

        public EditorViewMenuItemProvider(IDocumentManager documentManager)
        {
            SubItems = new List<IMenuItem>(capacity: documentManager.AllTools.Count);
            foreach (var window in documentManager.AllTools)
            {
                SubItems.Add(new ModuleMenuItem(window.Title, new DelegateCommand(() => documentManager.OpenTool(window.GetType()))));
            }
        }
    }
}