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
using WDE.MapSpawns.Models.Waypoints;
using WDE.QueryGenerators.Base;

namespace WDE.MapSpawns.Bridge.ViewModels;

/// <summary>The item stores only a (source, key) reference; the points shown here are the path's
/// CURRENT rows, loaded from the database when the document opens.</summary>
public class WaypointPathViewModel : BindableBase
{
    private string header;

    public WaypointPathViewModel(WaypointSource source, uint key, uint key2, string sourceName, string keyLabel, IDatabaseProvider databaseProvider)
    {
        header = $"{sourceName} {keyLabel} — loading...";
        Load(source, key, key2, sourceName, keyLabel, databaseProvider).ListenErrors();
    }

    private async Task Load(WaypointSource source, uint key, uint key2, string sourceName, string keyLabel, IDatabaseProvider databaseProvider)
    {
        var points = await WaypointDbLoader.LoadPoints(databaseProvider, source, key, key2);
        if (points == null || points.Count == 0)
        {
            Header = $"{sourceName} {keyLabel} — path deleted (or not readable)";
            return;
        }

        foreach (var point in points)
            Points.Add(point);
        Header = $"{sourceName} {keyLabel} — {points.Count} point{(points.Count == 1 ? "" : "s")}";
    }

    public string Header
    {
        get => header;
        private set => SetProperty(ref header, value);
    }

    public ObservableCollection<UniversalWaypoint> Points { get; } = new();
}

public class WaypointsDocumentViewModel : ReadOnlySpawnEditDocumentViewModel
{
    public WaypointsDocumentViewModel(WaypointsSolutionItem solutionItem,
        ISolutionItemSqlGeneratorRegistry sqlRegistry,
        IDatabaseProvider databaseProvider,
        IEnumerable<IWaypointSchemaInfoProvider> schemaInfoProviders) : base(solutionItem, sqlRegistry)
    {
        var source = (WaypointSource)solutionItem.Source;
        // the ACTUAL table name on the active core (e.g. waypoint_path, not waypoint_data, on master)
        var sourceName = source.ToName(schemaInfoProviders.FirstOrDefault());
        var keyLabel = solutionItem.Key2 == 0 ? $"#{solutionItem.Key}" : $"entry {solutionItem.Key} / path {solutionItem.Key2}";
        Title = $"Waypoints {sourceName} {keyLabel}";
        Paths = new List<WaypointPathViewModel>
        {
            new(source, solutionItem.Key, solutionItem.Key2, sourceName, keyLabel, databaseProvider)
        };
    }

    public IReadOnlyList<WaypointPathViewModel> Paths { get; }

    public override string Title { get; }
    public override ImageUri? Icon => new ImageUri("Icons/document_waypoints_big.png");
}
