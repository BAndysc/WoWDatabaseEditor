using System.Collections.Generic;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.Module.Attributes;

namespace WDE.LootEditor.Editor.Standalone;

[AutoRegister]
public class StandaloneLootEditorTopBarQuickAccessProvider : ITopBarQuickAccessProvider
{
    public IEnumerable<ITopBarQuickAccessItem> Items { get; set; }
    public int Order => -2;
    
    public StandaloneLootEditorTopBarQuickAccessProvider(ILootService lootService)
    {
        Items = new List<ITopBarQuickAccessItem>()
        {
            new TopBarQuickAccessItem("Loot", new ImageUri("Icons/document_loot.png"),
                new DelegateCommand(
                    () =>
                    {
                        lootService.OpenStandaloneLootEditor().ListenErrors();
                    }))
        };
    }
    
    private class TopBarQuickAccessItem : ITopBarQuickAccessItem
    {
        public TopBarQuickAccessItem(string name, ImageUri icon, ICommand command)
        {
            Name = name;
            Icon = icon;
            Command = command;
        }
        
        public ICommand Command { get; }
        public string Name { get; }
        public ImageUri Icon { get; }
    }
}