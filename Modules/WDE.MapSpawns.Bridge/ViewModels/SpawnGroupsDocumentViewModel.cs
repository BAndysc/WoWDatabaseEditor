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

public class SpawnGroupMemberRowViewModel
{
    public SpawnGroupMemberRowViewModel(ISpawnGroupSpawn member)
    {
        Type = member.Type == SpawnGroupTemplateType.GameObject ? "gameobject" : "creature";
        Guid = member.Guid;
    }

    public string Type { get; }
    public uint Guid { get; }
}

/// <summary>The item stores only the group id; the template name and members shown here are the
/// group's CURRENT rows, loaded from the database when the document opens.</summary>
public class SpawnGroupViewModel : BindableBase
{
    private string header;

    public SpawnGroupViewModel(uint groupId, IDatabaseProvider databaseProvider)
    {
        header = $"Group {groupId} — loading...";
        Load(groupId, databaseProvider).ListenErrors();
    }

    private async Task Load(uint groupId, IDatabaseProvider databaseProvider)
    {
        var template = await databaseProvider.GetSpawnGroupTemplateByIdAsync(groupId);
        if (template == null)
        {
            Header = $"Group {groupId} — deleted (or not readable)";
            return;
        }

        var members = (await databaseProvider.GetSpawnGroupSpawnsAsync())
            .Where(s => s.TemplateId == groupId)
            .ToList();
        foreach (var member in members)
            Members.Add(new SpawnGroupMemberRowViewModel(member));
        Header = $"Group {groupId} \"{template.Name}\" — {members.Count} member{(members.Count == 1 ? "" : "s")}";
    }

    public string Header
    {
        get => header;
        private set => SetProperty(ref header, value);
    }

    public ObservableCollection<SpawnGroupMemberRowViewModel> Members { get; } = new();
}

public class SpawnGroupsDocumentViewModel : ReadOnlySpawnEditDocumentViewModel
{
    public SpawnGroupsDocumentViewModel(SpawnGroupsSolutionItem solutionItem,
        ISolutionItemSqlGeneratorRegistry sqlRegistry,
        IDatabaseProvider databaseProvider) : base(solutionItem, sqlRegistry)
    {
        Title = $"Spawn group {solutionItem.GroupId}";
        Groups = new List<SpawnGroupViewModel>
        {
            new(solutionItem.GroupId, databaseProvider)
        };
    }

    public IReadOnlyList<SpawnGroupViewModel> Groups { get; }

    public override string Title { get; }
    public override ImageUri? Icon => new ImageUri("Icons/document_spawngroup_big.png");
}
