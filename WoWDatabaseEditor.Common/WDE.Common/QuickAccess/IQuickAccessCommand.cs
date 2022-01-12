using System;
using System.Threading;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.QuickAccess;

/**
 * Represents a slash command that can be executed from the quick access
 * if user starts a search with `/`, for example `/explain` a command with this name will
 * be executed. Only one command can be executed, contrary to IQuickAccessSearchProvider
 */
[NonUniqueProvider]
public interface IQuickAccessCommand
{
    string Command { get; }
    string? Help { get; }
    Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken);
}