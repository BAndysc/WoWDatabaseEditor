using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using WDE.Common.Database;
using WDE.Common.QuickAccess;
using WDE.Common.Services;
using WDE.Common.Services.FindAnywhere;
using WDE.Common.Types;
using WDE.Module.Attributes;

namespace WDE.Parameters.QuickAccess;

[AutoRegister]
public class FindAnywhereQuickAccess : IQuickAccessSearchProvider
{
    private readonly ICachedDatabaseProvider cachedDatabaseProvider;
    private readonly ISpellService spellService;

    private ICommand OpenFindAnywhereCreatureCommand { get; }

    private ICommand OpenFindAnywhereSpellCommand { get; }

    public FindAnywhereQuickAccess(ICachedDatabaseProvider cachedDatabaseProvider,
        IDbcSpellService spellService,
        IFindAnywhereService findAnywhereService,
        IQuickCommands quickCommands)
    {
        this.cachedDatabaseProvider = cachedDatabaseProvider;
        this.spellService = spellService;
        OpenFindAnywhereCreatureCommand = new DelegateCommand<uint?>(id =>
        {
            findAnywhereService.OpenFind(["CreatureParameter"], id ?? 0);
            quickCommands.CloseSearchCommand.Execute(null);
        });

        OpenFindAnywhereSpellCommand = new DelegateCommand<uint?>(id =>
        {
            findAnywhereService.OpenFind(["SpellParameter"], id ?? 0);
            quickCommands.CloseSearchCommand.Execute(null);
        });
    }

    public int Order => 0;

    public async Task Provide(string text, Action<QuickAccessItem> produce, CancellationToken cancellationToken)
    {
        if (!uint.TryParse(text, out var asNumber))
            return;

        if (cachedDatabaseProvider.GetCachedCreatureTemplate(asNumber) is { } creature)
            produce(new QuickAccessItem(new ImageUri("Icons/document_creatures.png"), $"Find usages of npc {creature.Name}", text, "Find anywhere", OpenFindAnywhereCreatureCommand, asNumber, 0));

        if (spellService.Exists(asNumber))
            produce(new QuickAccessItem(new ImageUri("Icons/document_spell.png"), $"Find usages of spell {spellService.GetName(asNumber)}", text, "Find anywhere", OpenFindAnywhereSpellCommand, asNumber, 0));
    }
}