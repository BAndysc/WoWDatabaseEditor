using System.Windows.Input;
using WDE.Common.Menu;

namespace WoWDatabaseEditorCore.Providers
{
    public class ModuleMenuItem: IMenuCommandItem
    {
        public string ItemName { get; }
        public ICommand ItemCommand { get; }

        public ModuleMenuItem(string itemName, ICommand itemCommand)
        {
            ItemName = itemName;
            ItemCommand = itemCommand;
        }
    }
}