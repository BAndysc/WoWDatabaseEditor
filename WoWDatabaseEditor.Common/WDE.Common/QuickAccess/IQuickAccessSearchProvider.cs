using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

/*
 * When a user searches for non command thing (doesn't start the command with `/`),
 * then all quick search providers will be executed
 */
[NonUniqueProvider]
public interface IQuickAccessSearchProvider
{
    Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken);
}

[UniqueProvider]
public interface IQuickCommands
{
    ICommand CopyCommand { get; }
    ICommand SetSearchCommand { get; }
    ICommand NoCommand { get; }
    ICommand CloseSearchCommand { get; }
    QuickAccessItem AndMoreItem { get; }
}