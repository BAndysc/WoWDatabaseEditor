using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Prism.Mvvm;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.MapSpawns.Models.Solution;

namespace WDE.MapSpawns.Bridge.ViewModels;

public class PoolMemberRowViewModel
{
    public PoolMemberRowViewModel(string type, string key, float chance, string? description)
    {
        Type = type;
        Key = key;
        Chance = chance == 0 ? "equal" : $"{chance:0.#}%";
        Description = description ?? "";
    }

    public string Type { get; }
    public string Key { get; }
    public string Chance { get; }
    public string Description { get; }
}

/// <summary>The item stores only the pool id; the template and members shown here are the pool's
/// CURRENT rows, loaded from the database when the document opens.</summary>
public class PoolViewModel : BindableBase
{
    private string header;

    public PoolViewModel(uint poolId, IDatabaseProvider databaseProvider)
    {
        header = $"Pool {poolId} — loading...";
        Load(poolId, databaseProvider).ListenErrors();
    }

    private async Task Load(uint poolId, IDatabaseProvider databaseProvider)
    {
        var template = await databaseProvider.GetPoolTemplateByIdAsync(poolId);
        if (template == null)
        {
            Header = $"Pool {poolId} — deleted (or not readable)";
            return;
        }

        int count = 0;
        foreach (var c in (await databaseProvider.GetPoolCreaturesAsync() ?? new List<IPoolCreatureMember>())
                 .Where(x => x.PoolEntry == poolId))
        {
            Members.Add(new PoolMemberRowViewModel("creature", $"guid {c.Guid}", c.Chance, c.Description));
            count++;
        }
        foreach (var g in (await databaseProvider.GetPoolGameObjectsAsync() ?? new List<IPoolGameObjectMember>())
                 .Where(x => x.PoolEntry == poolId))
        {
            Members.Add(new PoolMemberRowViewModel("gameobject", $"guid {g.Guid}", g.Chance, g.Description));
            count++;
        }
        foreach (var c in (await databaseProvider.GetPoolCreatureEntryPoolsAsync() ?? new List<IPoolCreatureEntryMember>())
                 .Where(x => x.PoolEntry == poolId))
        {
            Members.Add(new PoolMemberRowViewModel("creature", $"entry {c.Entry} (all spawns)", c.Chance, c.Description));
            count++;
        }
        foreach (var g in (await databaseProvider.GetPoolGameObjectEntryPoolsAsync() ?? new List<IPoolGameObjectEntryMember>())
                 .Where(x => x.PoolEntry == poolId))
        {
            Members.Add(new PoolMemberRowViewModel("gameobject", $"entry {g.Entry} (all spawns)", g.Chance, g.Description));
            count++;
        }
        foreach (var n in (await databaseProvider.GetPoolNestingsAsync() ?? new List<IPoolNesting>())
                 .Where(x => x.MotherPool == poolId))
        {
            Members.Add(new PoolMemberRowViewModel("pool", $"child pool {n.PoolId}", n.Chance, n.Description));
            count++;
        }

        string limit = template.MaxLimit == 0 ? "no limit" : $"max {template.MaxLimit} at once";
        Header = $"Pool {poolId} \"{template.Description}\" — {count} member{(count == 1 ? "" : "s")}, {limit}";
    }

    public string Header
    {
        get => header;
        private set => SetProperty(ref header, value);
    }

    public ObservableCollection<PoolMemberRowViewModel> Members { get; } = new();
}

public class PoolsDocumentViewModel : ReadOnlySpawnEditDocumentViewModel
{
    public PoolsDocumentViewModel(PoolsSolutionItem solutionItem,
        ISolutionItemSqlGeneratorRegistry sqlRegistry,
        IDatabaseProvider databaseProvider) : base(solutionItem, sqlRegistry)
    {
        Title = $"Spawn pool {solutionItem.PoolId}";
        Pools = new List<PoolViewModel>
        {
            new(solutionItem.PoolId, databaseProvider)
        };
    }

    public IReadOnlyList<PoolViewModel> Pools { get; }

    public override string Title { get; }
    public override ImageUri? Icon => new ImageUri("Icons/document_pool_big.png");
}
