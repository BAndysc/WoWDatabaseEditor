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

public class FormationMemberRowViewModel
{
    public FormationMemberRowViewModel(ICreatureFormation row)
    {
        MemberGuid = row.MemberGuid;
        Dist = row.Dist;
        Angle = row.Angle;
        GroupAi = row.GroupAi;
        Point1 = row.Point1;
        Point2 = row.Point2;
    }

    public uint MemberGuid { get; }
    public float Dist { get; }
    public float Angle { get; }
    public uint GroupAi { get; }
    public uint Point1 { get; }
    public uint Point2 { get; }
}

/// <summary>The item stores only the leader guid; the members shown here are the formation's CURRENT
/// creature_formations rows, loaded from the database when the document opens.</summary>
public class FormationGroupViewModel : BindableBase
{
    private string header;

    public FormationGroupViewModel(uint leaderGuid, IDatabaseProvider databaseProvider)
    {
        header = $"Leader {leaderGuid} — loading...";
        Load(leaderGuid, databaseProvider).ListenErrors();
    }

    private async Task Load(uint leaderGuid, IDatabaseProvider databaseProvider)
    {
        var rows = (await databaseProvider.GetCreatureFormations())
            .Where(f => f.LeaderGuid == leaderGuid)
            .ToList();
        if (rows.Count == 0)
        {
            Header = $"Leader {leaderGuid} — formation deleted (or not readable)";
            return;
        }

        foreach (var row in rows)
            Members.Add(new FormationMemberRowViewModel(row));
        Header = $"Leader {leaderGuid} — {rows.Count} member{(rows.Count == 1 ? "" : "s")}";
    }

    public string Header
    {
        get => header;
        private set => SetProperty(ref header, value);
    }

    public ObservableCollection<FormationMemberRowViewModel> Members { get; } = new();
}

public class FormationsDocumentViewModel : ReadOnlySpawnEditDocumentViewModel
{
    public FormationsDocumentViewModel(FormationsSolutionItem solutionItem,
        ISolutionItemSqlGeneratorRegistry sqlRegistry,
        IDatabaseProvider databaseProvider) : base(solutionItem, sqlRegistry)
    {
        Title = $"Formation of leader {solutionItem.LeaderGuid}";
        Groups = new List<FormationGroupViewModel>
        {
            new(solutionItem.LeaderGuid, databaseProvider)
        };
    }

    public IReadOnlyList<FormationGroupViewModel> Groups { get; }

    public override string Title { get; }
    public override ImageUri? Icon => new ImageUri("Icons/document_creature_summon_groups_big.png");
}
