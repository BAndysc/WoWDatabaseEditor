using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Menu;
using WDE.Common.Modules;
using WDE.Common.QuickAccess;
using WDE.Common.Tasks;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditor.Providers;

[AutoRegister]
public class MenuQuickAccessProvider : IQuickAccessSearchProvider
{
    private readonly Lazy<EditorMainMenuItemsProvider> mainMenuProvider;
    private readonly IQuickCommands quickCommands;
    private List<(string actionName, string parentCategory, string? shortcut, ICommand)> flattenedAllItems = new();

    public MenuQuickAccessProvider(Lazy<EditorMainMenuItemsProvider> mainMenuProvider,
        IMainThread mainThread,
        IQuickCommands quickCommands)
    {
        this.mainMenuProvider = mainMenuProvider;
        this.quickCommands = quickCommands;
        mainThread.Delay(() =>
        {
            flattenedAllItems = new();
            foreach (var mainMenu in this.mainMenuProvider.Value.GetItems())
            {
                foreach (var subItem in mainMenu.SubItems)
                {
                    if (subItem is IMenuCommandItem commandItem)
                    {
                        var cmd = new DelegateCommand(
                            () =>
                            {
                                if (commandItem.ItemCommand.CanExecute(null))
                                    commandItem.ItemCommand.Execute(null);
                                quickCommands.CloseSearchCommand.Execute(null);
                            }, () => commandItem.ItemCommand.CanExecute(null));
                        commandItem.ItemCommand.CanExecuteChanged += (_, __) => cmd.RaiseCanExecuteChanged();
                        flattenedAllItems.Add((subItem.ItemName.Replace("_", ""), mainMenu.ItemName.Replace("_", ""), commandItem.Shortcut?.InputShortcutText, cmd));
                    }
                }
            }
        }, TimeSpan.FromMilliseconds(100));
    }

    public int Order => 1001;

    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {

        foreach (var (action, parentName, shortcut, command) in flattenedAllItems)
        {
            if (action.Contains(text, StringComparison.OrdinalIgnoreCase) || parentName.Equals(text, StringComparison.OrdinalIgnoreCase))
            {
                if (command.CanExecute(null))
                {
                    produce(new QuickAccessItem(ImageUri.Empty, $"{parentName}  /  {action}",
                        shortcut  ?? "", "Menu", command,
                        null, 102));
                }
            }
        }
    }
}