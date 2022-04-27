using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.QuickAccess;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WoWDatabaseEditorCore.Services.QuickAccess;

[AutoRegister]
public class FlagQuickAccessCommand : IQuickAccessCommand
{
    private readonly IQuickCommands quickCommands;
    public string Command => "flag";
    public string? Help => "/flag <number> - split a number into binary flags";

    public FlagQuickAccessCommand(IQuickCommands quickCommands)
    {
        this.quickCommands = quickCommands;
    }
    
    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        if (long.TryParse(text, out var number))
        {
            for (int i = 0; i < 63; ++i)
            {
                if (((1L << i) & number) != 0)
                {
                    produce(new QuickAccessItem(ImageUri.Empty, (1L << i).ToString(), "(copy)", "", quickCommands.CopyCommand, (1L << i)));
                }
            }
        }
    }
}

[AutoRegister]
[SingleInstance]
public class QuickAccessService : IQuickAccessService
{
    private readonly IQuickCommands quickCommands;
    private readonly IList<IQuickAccessCommand> commands;
    private readonly IList<IQuickAccessSearchProvider> searchProviders;

    public QuickAccessService(IParameterFactory parameterFactory,
        IQuickCommands quickCommands,
        IEnumerable<IQuickAccessCommand> commands, 
        IEnumerable<IQuickAccessSearchProvider> searchProviders)
    {
        this.quickCommands = quickCommands;
        this.commands = commands.ToList();
        this.searchProviders = searchProviders.ToList();
        this.commands.Add(new HelpCommand(this));
    }

    private class HelpCommand : IQuickAccessCommand
    {
        private readonly QuickAccessService service;

        public HelpCommand(QuickAccessService quickAccessService)
        {
            this.service = quickAccessService;
        }

        public string Command => "help";
        public string? Help => "Prints all possible quick access commands";

        public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
        {
            foreach (var cmd in service.commands)
            {
                var commandText = "/" + cmd.Command + " ";
                produce(new QuickAccessItem(ImageUri.Empty, commandText, "", cmd.Help ?? "", service.quickCommands.SetSearchCommand, commandText));
            }
        }
    }
    
    public Task Search(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        if (text.StartsWith("/"))
        {
            var spaceIndex = text.IndexOf(' ');
            string command = "";
            if (spaceIndex == -1)
            {
                command = text.Substring(1);
                text = "";
            }
            else
            {
                command = text.Substring(1, spaceIndex - 1);
                text = text.Substring(spaceIndex + 1);
            }
            return SearchCommand(command, text, produce, cancellationToken);
        }
        else
            return SearchThings(text, produce, cancellationToken);
        
    }

    private async Task SearchThings(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        foreach (var provider in searchProviders)
        {
            await provider.Provide(text, produce, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
        }
    }

    private async Task SearchCommand(string command, string search, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        IQuickAccessCommand? bestMatch = null;
        int startsWith = 0;

        // find exact command match
        foreach (var commandProvider in commands)
        {
            if (commandProvider.Command == command)
            {
                bestMatch = commandProvider;
                startsWith = 1;
                break;
            }
        }

        // if no exact match, find best match using prefix
        if (bestMatch == null)
        {
            foreach (var commandProvider in commands)
            {
                if (commandProvider.Command.StartsWith(command))
                {
                    bestMatch = commandProvider;
                    startsWith++;
                }
            }
        }

        // if still no match, display possible commands
        if (bestMatch == null || startsWith != 1)
        {
            foreach (var commandProvider in commands)
            {
                if (commandProvider.Command.StartsWith(command))
                {
                    var commandText = "/" + commandProvider.Command + " ";
                    produce(new QuickAccessItem(ImageUri.Empty, commandText, "", commandProvider.Help ?? "", quickCommands.SetSearchCommand, commandText));
                }
            }
        }
        else
        {
            await bestMatch.Provide(search, produce, cancellationToken);
        }
    }
}